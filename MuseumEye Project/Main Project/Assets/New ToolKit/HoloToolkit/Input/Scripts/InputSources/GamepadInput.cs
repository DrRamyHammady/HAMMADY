﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using UnityEngine.XR.WSA.Input;

namespace HoloToolkit.Unity.InputModule
{
    public class GamepadInput : BaseInputSource
    {
        [Tooltip("Game pad button to press for air tap.")]
        public string GamePadButtonA = "Fire1";
        [Tooltip("Change this value to give a different source id to your controller.")]
        public uint GamePadId = 50000;

        protected override void Start()
        {
            base.Start();
        }

        private void Update()
        {
            if (Input.GetButtonDown(GamePadButtonA))
            {
                inputManager.RaiseInputClicked(this, GamePadId, 1);
            }
        }

        public override SupportedInputInfo GetSupportedInputInfo(uint sourceId)
        {
            // Since the game pad is not a 3dof or 6dof controller.
            return SupportedInputInfo.None;            
        }

        public override bool TryGetOrientation(uint sourceId, out Quaternion orientation)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetPointingRay(uint sourceId, out Ray pointingRay)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetPosition(uint sourceId, out Vector3 position)
        {
            throw new NotImplementedException();
        }
    }
}