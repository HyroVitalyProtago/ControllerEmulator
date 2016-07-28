using UnityEngine;
using UnityEngine.UI;
using System;

public class UpdateInfos : MonoBehaviour {

	[SerializeField] EmulatorServerSocket _ess = null;
	[SerializeField] Text _state = null, _ipPort = null;
	[SerializeField] Image _stateImage = null;
	[SerializeField] Color
		_initColor = Color.blue,
		_waitingColor = Color.red,
		_connectedColor = Color.green,
		_errorColor = Color.red;

	public void Update() {
		switch (_ess.Current) {
		case EmulatorServerSocket.State.Init:
			_state.text = "Init";
			_stateImage.color = _initColor;
			break;
		case EmulatorServerSocket.State.Waiting:
			_state.text = "Waiting for connection";
			_stateImage.color = _waitingColor;
			break;
		case EmulatorServerSocket.State.Connected:
			_state.text = "Connected";
			_stateImage.color = _connectedColor;
			break;
		case EmulatorServerSocket.State.Error:
			_state.text = "Error";
			_stateImage.color = _errorColor;
			break;
		default:
			throw new InvalidOperationException();	
		}

		_ipPort.text = "IP: " + _ess.Ip + ", port: " + _ess.Port;
	}
}
