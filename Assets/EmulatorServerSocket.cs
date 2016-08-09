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

class EmulatorServerSocket : MonoBehaviour {
    #region class_attributes
    static readonly int _port = 7003;
    static readonly string _portStr = _port.ToString();
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
    [SerializeField]
    GameObject _touchpad;
    Vector2 _touchpadPosition;
    Vector2 _touchpadNormalizedPositionToScreen;
    float _mapXStart, _mapXStop, _mapYStart, _mapYStop;
    float _touchpadSize;
    int _touchId;
    int _clickTouchId = -1;

    TcpListener _tcpServer;
    Thread _phoneEventThread;

    volatile bool shouldStop = false;

    public string Ip { get; private set; }
    public string Port { get { return _portStr; } }
    public State Current { get; private set; }

    readonly Entry[] _entries = new Entry[10];
    int _currentEntry;
    #endregion

    #region subtypes
    public enum State { Init, Waiting, Connected, Error }

    struct Entry {
        public int type;
        public int size;
        public byte[] bytes;
    }
    #endregion

    #region unity_callbacks
    void Awake() {
        Input.gyro.enabled = true;

        // init all entries
        for (int i = 0; i < _entries.Length; ++i) {
            _entries[i] = new Entry { bytes = new byte[64] };
        }
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
        lock (_entries.SyncRoot) {
            _currentEntry = 0;
        }

        UpdateOrientation();
        UpdateGyroscope();
        UpdateAccelerometer();
        UpdateMotion();

        // small heap with fast and frequent garbage collection
        // if (Time.frameCount % 30 == 0) { System.GC.Collect(); }
    }

    void OnDestroy() {
        shouldStop = true;

        StopServer();

        if (_phoneEventThread != null) {
            _phoneEventThread.Join();
        }
    }
    #endregion

    int TypeOfEvent(Type t) {
        if (t == typeof(PhoneEvent.Types.MotionEvent)) {
            return 1;
        } else if (t == typeof(PhoneEvent.Types.GyroscopeEvent)) {
            return 2;
        } else if (t == typeof(PhoneEvent.Types.AccelerometerEvent)) {
            return 3;
        } else if (t == typeof(PhoneEvent.Types.DepthMapEvent)) {
            return 4;
        } else if (t == typeof(PhoneEvent.Types.OrientationEvent)) {
            return 5;
        } else if (t == typeof(PhoneEvent.Types.KeyEvent)) {
            return 6;
        }
        throw new InvalidOperationException();
    }

    bool IsEntriesNotFull() { return _currentEntry < _entries.Length; }

    // TODO don't know exactly what this function should return...
    long GetTimestamp() { return (long)Time.realtimeSinceStartup; }

    void UpdateOrientation() {
        lock (_entries.SyncRoot) {
            if (IsEntriesNotFull()) {
                _entries[_currentEntry].type = TypeOfEvent(typeof(PhoneEvent.Types.OrientationEvent));
                _entries[_currentEntry].size = StaticStream
                    .Begin(_entries[_currentEntry].bytes)
                    .WriteInt32(8)
                    .WriteInt64(GetTimestamp())
                    .WriteInt32(21)
                    .WriteFloat(Input.gyro.attitude.x)
                    .WriteInt32(29)
                    .WriteFloat(Input.gyro.attitude.y)
                    .WriteInt32(37)
                    .WriteFloat(Input.gyro.attitude.z)
                    .WriteInt32(45)
                    .WriteFloat(-Input.gyro.attitude.w)
                    .End();

                ++_currentEntry;

                Monitor.PulseAll(_entries.SyncRoot);
            }
        }
    }
    void UpdateGyroscope() {
        lock (_entries.SyncRoot) {
            if (IsEntriesNotFull()) {
                _entries[_currentEntry].type = TypeOfEvent(typeof(PhoneEvent.Types.GyroscopeEvent));
                _entries[_currentEntry].size = StaticStream
                    .Begin(_entries[_currentEntry].bytes)
                    .WriteInt32(8)
                    .WriteInt64(GetTimestamp())
                    .WriteInt32(21)
                    .WriteFloat(Input.gyro.rotationRateUnbiased.x)
                    .WriteInt32(29)
                    .WriteFloat(Input.gyro.rotationRateUnbiased.y)
                    .WriteInt32(37)
                    .WriteFloat(Input.gyro.rotationRateUnbiased.z)
                    .End();

                ++_currentEntry;

                Monitor.PulseAll(_entries.SyncRoot);
            }
        }
    }
    void UpdateAccelerometer() {
        lock (_entries.SyncRoot) {
            if (IsEntriesNotFull()) {
                _entries[_currentEntry].type = TypeOfEvent(typeof(PhoneEvent.Types.AccelerometerEvent));
                _entries[_currentEntry].size = StaticStream
                    .Begin(_entries[_currentEntry].bytes)
                    .WriteInt32(8)
                    .WriteInt64(GetTimestamp())
                    .WriteInt32(21)
                    .WriteFloat(Input.acceleration.x)
                    .WriteInt32(29)
                    .WriteFloat(Input.acceleration.y)
                    .WriteInt32(37)
                    .WriteFloat(Input.acceleration.z)
                    .End();

                ++_currentEntry;

                Monitor.PulseAll(_entries.SyncRoot);
            }
        }
    }
    void UpdateMotion() {
        Touch[] touches = Input.touches;

        if (touches.Length <= 0) return;

        if (touches[0].fingerId == _touchId && touches[0].phase == TouchPhase.Moved &&
            touches[0].fingerId != _clickTouchId) {
            OnMotion(touches[0], EmulatorTouchEvent.Action.kActionMove);
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
                    lock (_entries.SyncRoot) {
                        while (!shouldStop) {

                            if (_currentEntry <= 0) {
                                Monitor.Wait(_entries.SyncRoot, 50);
                                continue;
                            }

                            while (_currentEntry > 0) {
                                Send(stream, ref _entries[--_currentEntry]);
                            }
                        }
                    }
                } catch (SocketException e) {
                    Debug.LogFormat("(continue) SocketException: {0}", e);
                } catch (IOException e) {
                    Debug.LogFormat("(continue) IOException: {0}", e);
                } finally {
                    client.Close();
                    print("client closed.");
                }
            }
        } catch (SocketException e) {
            Debug.LogFormat("(stop) SocketException: {0}", e);
        } catch (Exception e) {
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
        SendReversedFixedInt32(stream, entry.size + 4 /* for type length */); // send size
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
        lock (_entries.SyncRoot) {
            if (IsEntriesNotFull()) {
                _entries[_currentEntry].type = TypeOfEvent(typeof(PhoneEvent.Types.KeyEvent));
                _entries[_currentEntry].size = StaticStream
                    .Begin(_entries[_currentEntry].bytes)
                    .WriteInt32(8)
                    .WriteInt32((int)action)
                    .WriteInt32(16)
                    .WriteInt32((int)buttonCode)
                    .End();

                ++_currentEntry;

                Monitor.PulseAll(_entries.SyncRoot);
            }
        }
    }

    #region touch_utils
    float MapF(float value, float start1, float stop1, float start2, float stop2) {
        return start2 + (stop2 - start2) * ((value - start1) / (stop1 - start1));
    }

    Vector2 NormalizedPositionToScreen(Vector2 v) {
        return new Vector2(v.x / Screen.width, v.y / Screen.height);
    }

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
        return new Vector2(MapF(nv.x, _mapXStart, _mapXStop, 0, 1), MapF(nv.y, _mapYStart, _mapYStop, 1, 0));
    }

    bool InsideTouchpad(Vector2 v) {
        return Vector2.Distance(_touchpadPosition, v) < _touchpadSize;
    }
    #endregion

    public void OnMotionDown(BaseEventData bed) {
        Touch[] touches = Input.touches;

        if (touches.Length <= 0) return;

        if (touches[0].phase == TouchPhase.Began && InsideTouchpad(touches[0].position)) {

            if (touches[0].tapCount >= 2) {
                OnButton(EmulatorTouchEvent.Action.kActionDown, EmulatorButtonEvent.ButtonCode.kClick);
                _clickTouchId = touches[0].fingerId;
                return;
            }

            _touchId = touches[0].fingerId;

            OnMotion(touches[0], EmulatorTouchEvent.Action.kActionDown);
        }
    }

    public void OnTouchUp(BaseEventData bed) {
        Touch[] touches = Input.touches;

        if (touches.Length <= 0) return;

        if (_clickTouchId != -1) {
            if (touches[0].fingerId == _clickTouchId) {
                OnButton(EmulatorTouchEvent.Action.kActionUp, EmulatorButtonEvent.ButtonCode.kClick);
                _clickTouchId = -1;
            }
        }

        if (touches[0].fingerId == _touchId && touches[0].phase == TouchPhase.Ended) {
            OnMotion(touches[0], EmulatorTouchEvent.Action.kActionUp);
            _touchId = -1;
        }

    }

    void OnMotion(Touch touch, EmulatorTouchEvent.Action action) {
        lock (_entries.SyncRoot) {
            if (IsEntriesNotFull()) {
                Vector2 normalizedPositionToTouchpad = NormalizedPositionToTouchpad(touch.position);

                _entries[_currentEntry].type = TypeOfEvent(typeof(PhoneEvent.Types.MotionEvent));

                StaticStream staticStream = StaticStream
                    .Begin(_entries[_currentEntry].bytes)
                    .WriteInt32(8)
                    .WriteInt64(GetTimestamp())
                    .WriteInt32(16)
                    .WriteInt32((int)action)
                    .WriteInt32(26);

                int sizeOfPointerPosition = staticStream.Position;

                _entries[_currentEntry].size = staticStream
                    .WriteInt32(0) // size of pointer list not known
                    .WriteInt32(8)
                    .WriteInt32(touch.fingerId)
                    .WriteInt32(21)
                    .WriteFloat(normalizedPositionToTouchpad.x)
                    .WriteInt32(29)
                    .WriteFloat(normalizedPositionToTouchpad.y)
                    .End();

                // write the right size of pointer list
                staticStream
                    .SetPosition(sizeOfPointerPosition)
                    .WriteInt32(_entries[_currentEntry].size - sizeOfPointerPosition - 1 /* for the WriteFixed32 */);

                ++_currentEntry;

                Monitor.PulseAll(_entries.SyncRoot);
            }
        }
    }
    #endregion
}