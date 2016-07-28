using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Collections;
using System.IO;

using proto;
using Gvr.Internal;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// TODO
/// - show the correct IP
/// - send all events
///   - send accelerometer event
///   - send motion event
///   - send gyroscope event
///   - send key event
/// </summary>
class EmulatorServerSocket : MonoBehaviour {
	#region class_attributes
	static readonly int _port = 7003;

	// TODO properly
	static readonly byte[][] _types = new byte[][]{
		new byte[]{ 0x00, 0x00, 0x00, 0x00 }, // NOTHING
		new byte[]{ 0x08, 0x01, 0x12, 0x15 }, // MOTION
		new byte[]{ 0x08, 0x02, 0x1A, 0x19 }, // GYROSCOPE
		new byte[]{ 0x08, 0x03, 0x22, 0x19 }, // ACCELEROMETER
		new byte[]{ 0x08, 0x04, 0x00, 0x00 }, // DEPTH_MAP - @NotImplemented
		new byte[]{ 0x08, 0x05, 0x32, 0x1E }, // ORIENTATION
		new byte[]{ 0x08, 0x06, 0x3A, 0x04 } // KEY
	};

	static readonly float _normalizedTouchpadWidth = .3f, _normalizedTouchpadHeight = .2f;
	#endregion

	#region instance_attributes
	[SerializeField] GameObject _touchpad;
	Vector2 _touchpadPosition;
	Vector2 _touchpadNormalizedPositionToScreen;
	float _mapXStart, _mapXStop, _mapYStart, _mapYStop;
	float _touchpadSize;

	TcpListener _tcpServer;
	Thread _phoneEventThread;

	volatile bool shouldStop = false;

	public string Ip { get; private set; }
	public string Port { get { return _port.ToString(); } }
	public State Current { get; private set; }

	Queue _pendingEvents = Queue.Synchronized(new Queue());
	#endregion

	#region subtypes
	public enum State { Init, Waiting, Connected, Error }

	struct Entry {
		public int type;
		public byte[] bytes;
	}
	#endregion

	#region event_utils
	// TODO for each event type
	// refer to the enum PhoneEvent.Types.Type id
	int TypeOfEvent(PhoneEvent.Types.MotionEvent e) { return 1; }
	int TypeOfEvent(PhoneEvent.Types.GyroscopeEvent e) { return 2; }
	int TypeOfEvent(PhoneEvent.Types.AccelerometerEvent e) { return 3; }
	int TypeOfEvent(PhoneEvent.Types.DepthMapEvent e) { return 4; }
	int TypeOfEvent(PhoneEvent.Types.OrientationEvent e) { return 5; }
	int TypeOfEvent(PhoneEvent.Types.KeyEvent e) { return 6; }
	#endregion

	#region unity_callbacks
	void Awake() {
		Input.gyro.enabled = true;
	}

	void Start() {
		_touchpadPosition = ((RectTransform)_touchpad.transform).position;
		_touchpadNormalizedPositionToScreen = NormalizedPositionToScreen(_touchpadPosition);
		_mapXStart = _touchpadNormalizedPositionToScreen.x - _normalizedTouchpadWidth;
		_mapXStop = _touchpadNormalizedPositionToScreen.x + _normalizedTouchpadWidth;
		_mapYStart = _touchpadNormalizedPositionToScreen.y - _normalizedTouchpadHeight;
		_mapYStop = _touchpadNormalizedPositionToScreen.y + _normalizedTouchpadHeight;
		_touchpadSize = _normalizedTouchpadWidth * Screen.width;

		_phoneEventThread = new Thread(Loop); // to coroutines to be in the main thread
		_phoneEventThread.Start();
	}

	void Update() {
		UpdateOrientation();
		UpdateGyroscope();
		UpdateAccelerometer();
		// TODO UpdateMotion
	}

	void OnDestroy() {
		shouldStop = true;

		StopServer();

		if (_phoneEventThread != null) {
			_phoneEventThread.Join();
		}
	}
	#endregion

	long GetTimestamp() { return (long) Time.realtimeSinceStartup; }

	void UpdateOrientation() {
		var orientationEvent = PhoneEvent.Types.OrientationEvent
			.CreateBuilder()
			.SetTimestamp(GetTimestamp())
			.SetX(Input.gyro.attitude.x)
			.SetY(Input.gyro.attitude.y)
			.SetZ(Input.gyro.attitude.z)
			.SetW(-Input.gyro.attitude.w)
			.Build();

		Enqueue(new Entry {
			type = TypeOfEvent(orientationEvent),
			bytes = orientationEvent.ToByteArray()
		});
	}

	void UpdateGyroscope() {
		var gyroscopeEvent = PhoneEvent.Types.GyroscopeEvent
			.CreateBuilder()
			.SetTimestamp(GetTimestamp())
			.SetX(Input.gyro.rotationRateUnbiased.x)
			.SetY(Input.gyro.rotationRateUnbiased.y)
			.SetZ(Input.gyro.rotationRateUnbiased.z)
			.Build();

		Enqueue(new Entry {
			type = TypeOfEvent(gyroscopeEvent),
			bytes = gyroscopeEvent.ToByteArray()
		});
	}

	void UpdateAccelerometer() {
		var accelerometerEvent = PhoneEvent.Types.AccelerometerEvent
			.CreateBuilder()
			.SetTimestamp(GetTimestamp())
			.SetX(Input.acceleration.x)
			.SetY(Input.acceleration.y)
			.SetZ(Input.acceleration.z)
			.Build();

		Enqueue(new Entry {
			type = TypeOfEvent(accelerometerEvent),
			bytes = accelerometerEvent.ToByteArray()
		});
	}

	void Enqueue(Entry e) {
		lock (_pendingEvents.SyncRoot) {
			_pendingEvents.Enqueue(e);
			Monitor.PulseAll(_pendingEvents.SyncRoot);
		}
	}

	#region network_thread
	void Loop() {
		try {
			_tcpServer = new TcpListener(IPAddress.Any, _port);
			_tcpServer.Start();

			// Get the current ip and notify state change
			var ips = (Dns.GetHostEntry(Dns.GetHostName())).AddressList.ToList();
//			ips.ForEach(tmp => Debug.LogFormat("{0} {1}",tmp.AddressFamily, tmp.ToString()));
			var ip = ips.Last(x => x.AddressFamily == AddressFamily.InterNetwork && x.ToString() != "0.0.0.0");
			if (ip == null) {
				throw new InvalidOperationException();
			} else {
				Ip = ip.ToString();
			}

			while (!shouldStop) {
				print("waiting for connection");
				Current = State.Waiting;

				TcpClient client = _tcpServer.AcceptTcpClient();

				print("connected");
				Current = State.Connected;

				NetworkStream stream = client.GetStream();
				stream.WriteTimeout = 10; // 5000; // ms
				try {
					lock (_pendingEvents.SyncRoot) {
						while (!shouldStop) {
						
							if (_pendingEvents.Count <= 0) {
								Monitor.Wait(_pendingEvents.SyncRoot, 50);
								continue;
							}

							while (_pendingEvents.Count > 0) {
								var entry = (Entry) _pendingEvents.Dequeue();
								Send(stream, entry.type, entry.bytes);
							}
						}
					}
				} catch(SocketException e) {
					Debug.LogFormat("(continue) SocketException: {0}", e);
				} catch(IOException e) {
					Debug.LogFormat("(continue) IOException: {0}", e);
				} finally {
					client.Close();
					print("client closed.");
				}
			}
		} catch(SocketException e) {
			Debug.LogFormat("(stop) SocketException: {0}", e);
		} catch(Exception e) {
			Debug.LogFormat("(stop) Exception: {0}", e);
			throw e;
		} finally {
			StopServer();
		}
	}

	void Send(Stream stream, int type, byte[] buffer) {
		byte[] bufferSize = BitConverter.GetBytes(buffer.Length + 4); // + 4 for type length
		Array.Reverse(bufferSize);

		// send length (on 4 bytes)
		stream.Write(bufferSize, 0, bufferSize.Length); // length: 4

		// send type (on 4 bytes)
		// 0x08, phoneEvent.Type, _phoneEventFieldTags[], ???
		stream.Write(_types[type], 0, 4);

		// send event (on length bytes)
		stream.Write(buffer, 0, buffer.Length);
	}

	void StopServer() {
		if (_tcpServer != null) {
			_tcpServer.Stop();
			_tcpServer = null;
		}

		print("server stopped.");
		Current = State.Error;
	}
	#endregion

	#region event_interface
	public void OnHomeDown() { OnHome(EmulatorTouchEvent.Action.kActionDown); }
	public void OnHomeUp() { OnHome(EmulatorTouchEvent.Action.kActionUp); }
	public void OnHomeCancel() { OnHome(EmulatorTouchEvent.Action.kActionCancel); }
	void OnHome(EmulatorTouchEvent.Action action) { OnButton(action, EmulatorButtonEvent.ButtonCode.kHome); }

	public void OnAppDown() { OnApp(EmulatorTouchEvent.Action.kActionDown); }
	public void OnAppUp() { OnApp(EmulatorTouchEvent.Action.kActionUp); }
	public void OnAppCancel() { OnApp(EmulatorTouchEvent.Action.kActionCancel); }
	void OnApp(EmulatorTouchEvent.Action action) { OnButton(action, EmulatorButtonEvent.ButtonCode.kApp); }

	void OnButton(EmulatorTouchEvent.Action action, EmulatorButtonEvent.ButtonCode buttonCode) {
		var btn = PhoneEvent.Types.KeyEvent
			.CreateBuilder()
			.SetAction((int) action)
			.SetCode((int) buttonCode)
			.Build();
		Enqueue(new Entry {
			type = TypeOfEvent(btn),
			bytes = btn.ToByteArray()
		});
	}

	#region touch_utils
	float MapF(float value, float start1, float stop1, float start2, float stop2) {
		return start2 + (stop2 - start2) * ((value - start1) / (stop1 - start1));
	}
	Vector2 NormalizedPositionToScreen(Vector2 v) { return new Vector2(v.x / Screen.width, v.y / Screen.height); }
	Vector2 NormalizedPositionToTouchpad(Vector2 v, bool alreadyNormalized = false) {
		Vector2 nv = alreadyNormalized ? v : NormalizedPositionToScreen(v);
		return new Vector2(MapF(nv.x,_mapXStart,_mapXStop,0,1), MapF(nv.y,_mapYStart,_mapYStop,1,0));
	}
	bool InsideTouchpad(Vector2 v) { return Vector2.Distance(_touchpadPosition, v) < _touchpadSize; }
	IEnumerable<PhoneEvent.Types.MotionEvent.Types.Pointer> TouchesToPointers(IEnumerable<Touch> touches) {
		return touches.Select(t => {
			Vector2 normalizedPositionToTouchpad = NormalizedPositionToTouchpad(t.position);
			return PhoneEvent.Types.MotionEvent.Types.Pointer
				.CreateBuilder()
				.SetId(t.fingerId)
				.SetNormalizedX(normalizedPositionToTouchpad.x)
				.SetNormalizedY(normalizedPositionToTouchpad.y)
				.Build();
		});
	}
	#endregion

	// TODO keep trace of fingers id to do better things
	// TODO check touches into update for MOTION

	public void OnMotionDown(BaseEventData bed) {
//		Vector2 normalizedMousePositionToTouchpad = NormalizedPositionToTouchpad(Input.mousePosition);
//		Debug.LogFormat("{0} < {1}", Vector2.Distance(_touchpadPosition, Input.mousePosition), _normalizedTouchpadWidth * Screen.width);
//		Debug.LogFormat("normalizedMousePositionInTouchpad(x:{0}, y:{1})",normalizedMousePositionToTouchpad.x, normalizedMousePositionToTouchpad.y);

		var touches = Input.touches.Where(t => t.phase == TouchPhase.Began && InsideTouchpad(t.position) && t.tapCount >= 2);
		if (touches.Any()) {
			OnButton(EmulatorTouchEvent.Action.kActionDown, EmulatorButtonEvent.ButtonCode.kClick);
			OnButton(EmulatorTouchEvent.Action.kActionUp, EmulatorButtonEvent.ButtonCode.kClick); // TODO
		}

		OnMotion(TouchPhase.Began, EmulatorTouchEvent.Action.kActionDown);
	}
	public void OnMotionUp(BaseEventData bed) {
		OnMotion(TouchPhase.Ended, EmulatorTouchEvent.Action.kActionUp);
	}
	public void OnMotionMove(BaseEventData bed) {
		OnMotion(TouchPhase.Moved, EmulatorTouchEvent.Action.kActionMove);
	}
	public void OnMotionCancel(BaseEventData bed) {
		OnMotion(TouchPhase.Canceled, EmulatorTouchEvent.Action.kActionCancel);
	}
	void OnMotion(TouchPhase phase, EmulatorTouchEvent.Action action) {
		// Test with mouse
//		Vector2 normalizedPositionToTouchpad = NormalizedPositionToTouchpad(Input.mousePosition);
//		PhoneEvent.Types.MotionEvent.Types.Pointer[] touches = { PhoneEvent.Types.MotionEvent.Types.Pointer
//				.CreateBuilder()
//				.SetId(0)
//				.SetNormalizedX(normalizedPositionToTouchpad.x)
//				.SetNormalizedY(normalizedPositionToTouchpad.y)
//				.Build()
//		};

		var touches = Input.touches.Where(t => t.phase == phase && InsideTouchpad(t.position));
		if (!touches.Any()) return;

		var motion = PhoneEvent.Types.MotionEvent
			.CreateBuilder()
			.SetTimestamp(GetTimestamp())
			.SetAction((int) action)
			.AddRangePointers(TouchesToPointers(touches))
			.Build();

		Enqueue(new Entry {
			type = TypeOfEvent(motion),
			bytes = motion.ToByteArray()
		});
	}
	#endregion
}