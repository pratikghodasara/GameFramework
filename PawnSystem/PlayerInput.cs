﻿using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerInput {
    public delegate void AxisHandler(float value);
    public delegate void ActionHandler();

    Dictionary<string, AxisHandler> _axisHandlers = new Dictionary<string, AxisHandler>();
    Dictionary<string, ActionHandler> _actionHandlers = new Dictionary<string, ActionHandler>();

    public void BindAction(string actionName, ActionHandler handler) {
        if (_actionHandlers.ContainsKey(actionName)) {
            _actionHandlers[actionName] += handler;
        }
        else {
            _actionHandlers[actionName] = handler;
        }
    }

    public void BindAxis(string axisName, AxisHandler handler) {
        if (_axisHandlers.ContainsKey(axisName)) {
            _axisHandlers[axisName] += handler;
        }
        else {
            _axisHandlers[axisName] = handler;
        }
    }

    public void Tick() {
        TickActions();
        TickAxis();
    }

    void TickActions() {
        foreach (var actionHandler in _actionHandlers) {
            if (Input.GetAxis(actionHandler.Key) == 0)
                continue;

            actionHandler.Value();
        }
    }

    void TickAxis() {
        foreach (var axisMapping in _axisHandlers) {
            var rawValue = Input.GetAxisRaw(axisMapping.Key);

            axisMapping.Value(rawValue);
        }
    }
}
