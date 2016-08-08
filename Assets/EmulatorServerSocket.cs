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

using Debug = UnityEngine.Debug;
using Google.ProtocolBuffers;
using System.Reflection;

class EmulatorServerSocket : MonoBehaviour {
	#region class_attributes
	static readonly int _port = 7003;
	static readonly string _portStr = _port.ToString();

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
	List<int> _touchesId = new List<int>();
	int _clickTouchId = -1;

	TcpListener _tcpServer;
	Thread _phoneEventThread;

	volatile bool shouldStop = false;

	public string Ip { get; private set; }
	public string Port { get { return _portStr; } }
	public State Current { get; private set; }

	Queue _pendingEvents = Queue.Synchronized(new Queue());

	Entry[] _entries = new Entry[10];
	int _currentEntry;

//	PhoneEvent.Types.OrientationEvent orientationEvent;
//	FieldInfo orientationEventXFieldInfo;
	#endregion

	#region subtypes
	public enum State { Init, Waiting, Connected, Error }

	struct Entry {
		public int type;
		public int size;
		public byte[] bytes;
		public Stream stream;
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

	void OrientationEventEntryFill(ref Entry entry, long timestamp, float x, float y, float z, float w) {
		entry.type = 5;

		// Value on X bytes
//		entry.stream.Position = 0;
//		orientationEvent.WriteTo(entry.stream); // IMessageLite

		CustomCodedOutputStream.Bytes = entry.bytes;
		CustomCodedOutputStream.Position = 0;

		CustomCodedOutputStream.WriteInt32(8);
		CustomCodedOutputStream.WriteInt64(timestamp);

		CustomCodedOutputStream.WriteInt32(21);
		CustomCodedOutputStream.WriteFloat(x);

		CustomCodedOutputStream.WriteInt32(29);
		CustomCodedOutputStream.WriteFloat(y);

		CustomCodedOutputStream.WriteInt32(37);
		CustomCodedOutputStream.WriteFloat(z);

		CustomCodedOutputStream.WriteInt32(45);
		CustomCodedOutputStream.WriteFloat(w);

		// Message length on 4 bytes (reversed int32)
		entry.size = CustomCodedOutputStream.Position; // (int) entry.stream.Position;
	}
	#endregion

	#region unity_callbacks
	void Awake() {
		Input.gyro.enabled = true;

		// init all entries
		for (int i = 0; i < _entries.Length; ++i) {
			_entries[i] = new Entry();
			_entries[i].bytes = new byte[64];
			_entries[i].stream = new MemoryStream(_entries[i].bytes);
		}

//		orientationEvent = PhoneEvent.Types.OrientationEvent
//			.CreateBuilder()
//			.SetTimestamp(GetTimestamp())
//			.SetX(Input.gyro.attitude.x)
//			.SetY(Input.gyro.attitude.y)
//			.SetZ(Input.gyro.attitude.z)
//			.SetW(-Input.gyro.attitude.w)
//			.Build();
//
//		orientationEventXFieldInfo = typeof(PhoneEvent.Types.OrientationEvent)
//			.GetField("x_", BindingFlags.Instance | BindingFlags.NonPublic);
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
		_currentEntry = 0;

		UpdateOrientation();
//		UpdateGyroscope();
//		UpdateAccelerometer();
//		UpdateMotion();

		// small heap with fast and frequent garbage collection
//		if (Time.frameCount % 30 == 0) {
//			System.GC.Collect();
//		}
	}

	void OnDestroy() {
		shouldStop = true;

		StopServer();

		if (_phoneEventThread != null) {
			_phoneEventThread.Join();
		}
	}
	#endregion

	bool IsEntriesNotFull() { return _currentEntry < _entries.Length; }

	// TODO don't know exactly what this function should return...
	long GetTimestamp() { return (long) Time.realtimeSinceStartup; }

	void UpdateOrientation() {
//		PhoneEvent.Types.OrientationEvent orientationEvent = PhoneEvent.Types.OrientationEvent
//			.CreateBuilder()
//			.SetTimestamp(GetTimestamp())
//			.SetX(Input.gyro.attitude.x)
//			.SetY(Input.gyro.attitude.y)
//			.SetZ(Input.gyro.attitude.z)
//			.SetW(-Input.gyro.attitude.w)
//			.Build();
//
//		Enqueue(new Entry {
//			type = TypeOfEvent(orientationEvent),
//			bytes = orientationEvent.ToByteArray()
//		});

		lock (_entries.SyncRoot) {
			if (IsEntriesNotFull()) {
				OrientationEventEntryFill(
					ref _entries[_currentEntry++],
					timestamp: GetTimestamp(),
					x: Input.gyro.attitude.x,
					y: Input.gyro.attitude.y,
					z: Input.gyro.attitude.z,
					w: -Input.gyro.attitude.w
				);
				Monitor.PulseAll(_entries.SyncRoot);
			}
		}

		//			print(BitConverter.ToString(
		//				PhoneEvent.Types.OrientationEvent
		//				.CreateBuilder()
		//				.SetTimestamp(GetTimestamp())
		//				.SetX(Input.gyro.attitude.x)
		//				.SetY(Input.gyro.attitude.y)
		//				.SetZ(Input.gyro.attitude.z)
		//				.SetW(-Input.gyro.attitude.w)
		//				.Build()
		//				.ToByteArray()
		//			));
		//			print(BitConverter.ToString(_entries[_currentEntry - 1].bytes));
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

	void UpdateMotion() {
		List<TouchData> touches = Input.touches.Select(t => new TouchData(t)).ToList();

		#if UNITY_EDITOR // Mouse Test
		if (Input.GetMouseButton(0)) touches.Add(new TouchData(0, TouchPhase.Moved, Input.mousePosition));
		#endif

		if (touches.Count <= 0) return;

		IEnumerable<TouchData> touchpadTouches = touches.Where(t => _touchesId.Contains(t.fingerId));
		if (!touchpadTouches.Any()) return;

		IEnumerable<TouchData> movedTouches = touchpadTouches.Where(t => t.phase == TouchPhase.Moved && t.fingerId != _clickTouchId);
		if (movedTouches.Any()) {
			OnMotion(movedTouches, EmulatorTouchEvent.Action.kActionMove);
		}
	}

	// encapsulating Touch for debugging purpose....
	struct TouchData {
		public int fingerId { get; private set; }
		public TouchPhase phase { get; private set; }
		public Vector2 position { get; private set; }
		public int tapCount { get; private set; }
		public TouchData(int fingerId = 0, TouchPhase phase = TouchPhase.Began, Vector2 position = default(Vector2), int tapCount = 1) {
			this.fingerId = fingerId;
			this.phase = phase;
			this.position = position;
			this.tapCount = tapCount;
		}
		public TouchData(Touch touch) : this(touch.fingerId, touch.phase, touch.position, touch.tapCount) {}
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
//					lock (_pendingEvents.SyncRoot) {
//						while (!shouldStop) {
//						
//							if (_pendingEvents.Count <= 0) {
//								Monitor.Wait(_pendingEvents.SyncRoot, 50);
//								continue;
//							}
//
//							while (_pendingEvents.Count > 0) {
//								var entry = (Entry) _pendingEvents.Dequeue();
//								Send(stream, entry.type, entry.bytes);
//							}
//						}
//					}

					lock (_entries.SyncRoot) {
						while (!shouldStop) {

							if (_currentEntry <= 0) {
								Monitor.Wait(_entries.SyncRoot, 50);
								continue;
							}

							while (_currentEntry > 0) {
								Entry entry = (Entry) _entries[--_currentEntry];
								Send(stream, ref entry);
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

	void Send(Stream stream, ref Entry entry) {
		SendReversedFixedInt32(stream, entry.size); // send size
		stream.Write(_types[entry.type], 0, 4); // send type
		stream.Write(entry.bytes, 0, entry.size); // send value
	}

	void SendReversedFixedInt32(Stream stream, int value) {
		stream.WriteByte((byte)(value >> 24));
		stream.WriteByte((byte)(value >> 16));
		stream.WriteByte((byte)(value >> 8));
		stream.WriteByte((byte)(value));
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
//		var btn = PhoneEvent.Types.KeyEvent
//			.CreateBuilder()
//			.SetAction((int) action)
//			.SetCode((int) buttonCode)
//			.Build();
//		Enqueue(new Entry {
//			type = TypeOfEvent(btn),
//			bytes = btn.ToByteArray()
//		});
	}

	#region touch_utils
	float MapF(float value, float start1, float stop1, float start2, float stop2) {
		return start2 + (stop2 - start2) * ((value - start1) / (stop1 - start1));
	}

	Vector2 NormalizedPositionToScreen(Vector2 v) { return new Vector2(v.x / Screen.width, v.y / Screen.height); }

	Vector2 NormalizedPositionToTouchpad(Vector2 v) {
		// v is clamped to the touchpad
		if (Vector2.Distance(_touchpadPosition, v) > _touchpadSize) {
			v = _touchpadPosition + (v - _touchpadPosition).normalized * _touchpadSize;
		}

		// v is normalized according to the screen size
		Vector2 nv = NormalizedPositionToScreen(v);

		// v is normalized inside the touchpad
		//   0
		// 0 + 1
		//   1
		return new Vector2(MapF(nv.x,_mapXStart,_mapXStop,0,1), MapF(nv.y,_mapYStart,_mapYStop,1,0));
	}

	bool InsideTouchpad(Vector2 v) { return Vector2.Distance(_touchpadPosition, v) < _touchpadSize; }

	IEnumerable<PhoneEvent.Types.MotionEvent.Types.Pointer> TouchesToPointers(IEnumerable<TouchData> touches) {
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

	public void OnMotionDown(BaseEventData bed) {
		if (true) return; // TEST

		IEnumerable<TouchData> touchesData = Input.touches.Select(t => new TouchData(t));

		// Detect new touches on touchpad
		List<TouchData> touches = touchesData.Where(t => t.phase == TouchPhase.Began && InsideTouchpad(t.position)).ToList();

		#if UNITY_EDITOR // Mouse Test
		if (InsideTouchpad(Input.mousePosition)) {
			touches.Add(new TouchData(0, TouchPhase.Began, Input.mousePosition, Input.GetMouseButton(1) ? 2 : 1));
		}
		#endif

		if (!touches.Any()) return; // there is nothing to do

		// Is there a click?
		IEnumerable<TouchData> clickTouches = touches.Where(t => t.tapCount >= 2);
		if (clickTouches.Any()) {
			OnButton(EmulatorTouchEvent.Action.kActionDown, EmulatorButtonEvent.ButtonCode.kClick);

			var clickTouch = clickTouches.First();
			_clickTouchId = clickTouch.fingerId;
			touches.Remove(clickTouch); // not detected as a motion event
		}

		if (!touches.Any()) return; // don't send empty event (if one touch was a click, it is now removed)

		// Stock ids of touches
		_touchesId = _touchesId.Union(touches.Select(t => t.fingerId)).ToList();

		// Send corresponding event
		OnMotion(touches, EmulatorTouchEvent.Action.kActionDown);
	}

	public void OnTouchUp(BaseEventData bed) {
		if (true) return; // TEST

		List<TouchData> touches = Input.touches.Select(t => new TouchData(t)).ToList();

		#if UNITY_EDITOR // Mouse Test
		touches.Add(new TouchData(0, TouchPhase.Ended, Input.mousePosition));
		#endif

		// Click is released ?
		if (_clickTouchId != -1) {
			IEnumerable<TouchData> clickTouches = touches.Where(t => t.fingerId == _clickTouchId);
			if (clickTouches.Any()) {
				OnButton(EmulatorTouchEvent.Action.kActionUp, EmulatorButtonEvent.ButtonCode.kClick);
				_clickTouchId = -1;
			}
		}

		// Considering only touches who began on touchpad
		IEnumerable<TouchData> touchpadTouches = touches.Where(t => _touchesId.Contains(t.fingerId));
		if (!touchpadTouches.Any()) return;

		// Send corresponding event
		IEnumerable<TouchData> endedTouches = touchpadTouches.Where(t => t.phase == TouchPhase.Ended);
		if (endedTouches.Any()) {
			OnMotion(endedTouches, EmulatorTouchEvent.Action.kActionUp);
			_touchesId.RemoveAll(tid => endedTouches.Any(t => t.fingerId == tid)); // remove released touches
		}
	}

	void OnMotion(IEnumerable<TouchData> touches, EmulatorTouchEvent.Action action) {
//		var motion = PhoneEvent.Types.MotionEvent
//			.CreateBuilder()
//			.SetTimestamp(GetTimestamp())
//			.SetAction((int) action)
//			.AddRangePointers(TouchesToPointers(touches))
//			.Build();
//
//		Enqueue(new Entry {
//			type = TypeOfEvent(motion),
//			bytes = motion.ToByteArray()
//		});
	}
	#endregion
}