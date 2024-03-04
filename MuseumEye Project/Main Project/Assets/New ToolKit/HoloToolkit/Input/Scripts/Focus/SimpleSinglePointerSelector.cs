﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// An object which follows simplistic rules to choose 
    /// </summary>
    // TODO: robertes: write up the rules once we see how they feel in action and finalize them.
    // TODO: robertes: comment for HoloToolkit release.
    public class SimpleSinglePointerSelector :
        MonoBehaviour,
        ISourceStateHandler,
        IInputClickHandler,
        IInputHandler
    {
        #region Settings

        [Tooltip("The stabilizer, if any, used to smooth out controller ray data.")]
        public BaseRayStabilizer ControllerPointerStabilizer;

        [Tooltip("The cursor, if any, which should follow the selected pointer.")]
        public Cursor Cursor;

        [Tooltip("True to search for a cursor if one isn't explicitly set.")]
        public bool SearchForCursorIfUnset = true;
        
        #endregion

        #region Data

        private bool started = false;

        private bool addedInputManagerListener = false;
        private IPointingSource currentPointer = null;

        private readonly InputSourcePointer inputSourcePointer = new InputSourcePointer();

        #endregion

        #region MonoBehaviour Implementation

        private void Start()
        {
            started = true;

            if (InputManager.Instance == null)
            {
                Debug.LogError("InputManager is required.");
            }

            if (GazeManager.Instance == null)
            {
                Debug.LogError("GazeManager is required.");
            }

            if (FocusManager.Instance == null)
            {
                Debug.LogError("FocusManager is required.");
            }

            AddInputManagerListenerIfNeeded();
            FindCursorIfNeeded();
            ConnectBestAvailablePointer();
            
            Debug.Assert(currentPointer != null);
        }

        private void OnEnable()
        {
            if (started)
            {
                AddInputManagerListenerIfNeeded();
            }
        }

        private void OnDisable()
        {
            RemoveInputManagerListenerIfNeeded();
        }

        #endregion

        #region Input Event Handlers

        void ISourceStateHandler.OnSourceDetected(SourceStateEventData eventData)
        {
            if (IsGazePointerActive && SupportsPointingRay(eventData.InputSource, eventData.SourceId))
            {
                AttachInputSourcePointer(eventData);
                SetPointer(inputSourcePointer);
            }
        }

        void ISourceStateHandler.OnSourceLost(SourceStateEventData eventData)
        {
            if (IsInputSourcePointerActive && inputSourcePointer.InputIsFromSource(eventData))
            {
                ConnectBestAvailablePointer();
            }
        }

        void IInputClickHandler.OnInputClicked(InputClickedEventData eventData)
        {
            HandleInputAction(eventData);
        }

        void IInputHandler.OnInputUp(InputEventData eventData)
        {
            // Nothing to do on input up.
        }

        void IInputHandler.OnInputDown(InputEventData eventData)
        {
            HandleInputAction(eventData);
        }

        #endregion

        #region Utilities

        private void AddInputManagerListenerIfNeeded()
        {
            if (!addedInputManagerListener)
            {
                InputManager.Instance.AddGlobalListener(gameObject);
                addedInputManagerListener = true;
            }
        }

        private void RemoveInputManagerListenerIfNeeded()
        {
            if (addedInputManagerListener)
            {
                InputManager.Instance.RemoveGlobalListener(gameObject);
                addedInputManagerListener = false;
            }
        }

        private void FindCursorIfNeeded()
        {
            if ((Cursor == null) && SearchForCursorIfUnset)
            {
                Debug.LogWarningFormat(
                    "Cursor hasn't been explicitly set on \"{0}.{1}\". We'll search for a cursor in the hierarchy, but"
                        + " that comes with a performance cost, so it would be best if you explicitly set the cursor.",
                    name,
                    GetType().Name
                    );

                Cursor[] foundCursors = GameObject.FindObjectsOfType<Cursor>();

                if ((foundCursors == null) || (foundCursors.Length == 0))
                {
                    Debug.LogErrorFormat("Couldn't find cursor for \"{0}.{1}\".", name, GetType().Name);
                }
                else if (foundCursors.Length > 1)
                {
                    Debug.LogErrorFormat(
                        "Found more than one ({0}) cursors for \"{1}.{2}\", so couldn't automatically set one.",
                        foundCursors.Length,
                        name,
                        GetType().Name
                        );
                }
                else
                {
                    Cursor = foundCursors[0];
                }
            }
        }

        private void SetPointer(IPointingSource newPointer)
        {
            if (currentPointer != newPointer)
            {
                if (currentPointer != null)
                {
                    FocusManager.Instance.UnregisterPointer(currentPointer);
                }

                currentPointer = newPointer;

                if (newPointer != null)
                {
                    FocusManager.Instance.RegisterPointer(newPointer);
                }

                if (Cursor != null)
                {
                    Cursor.Pointer = newPointer;
                }
            }
        }

        private void ConnectBestAvailablePointer()
        {
            IPointingSource bestPointer = null;

            foreach (var detectedSource in InputManager.Instance.DetectedInputSources)
            {
                if (SupportsPointingRay(detectedSource))
                {
                    AttachInputSourcePointer(detectedSource);
                    bestPointer = inputSourcePointer;
                    break;
                }
            }

            if (bestPointer == null)
            {
                bestPointer = GazeManager.Instance;
            }

            SetPointer(bestPointer);
        }

        private void HandleInputAction(InputEventData eventData)
        {
            // TODO: robertes: Investigate how this feels. Since "Down" will often be followed by "Click", is
            //       marking the event as used actually effective in preventing unintended app input during a
            //       pointer change?

            bool pointerWasChanged;

            if (SupportsPointingRay(eventData))
            {
                if (IsInputSourcePointerActive && inputSourcePointer.InputIsFromSource(eventData))
                {
                    pointerWasChanged = false;
                }
                else
                {
                    AttachInputSourcePointer(eventData);
                    pointerWasChanged = true;
                }
            }
            else
            {
                if (IsGazePointerActive)
                {
                    pointerWasChanged = false;
                }
                else
                {
                    // TODO: robertes: see if we can treat voice separately from the other simple committers,
                    //       so voice doesn't steal from a pointing controller. I think input Kind would need
                    //       to come through with the event data.

                    SetPointer(GazeManager.Instance);
                    pointerWasChanged = true;
                }
            }

            if (pointerWasChanged)
            {
                // Since this input resulted in a pointer change, we mark the event as used to
                // prevent it from falling through to other handlers to prevent potentially
                // unintended input from reaching handlers that aren't being pointed at by
                // the new pointer.
                eventData.Use();
            }
        }

        private bool SupportsPointingRay(BaseInputEventData eventData)
        {
            return SupportsPointingRay(eventData.InputSource, eventData.SourceId);
        }

        private bool SupportsPointingRay(InputSourceInfo source)
        {
            return SupportsPointingRay(source.InputSource, source.SourceId);
        }

        private bool SupportsPointingRay(IInputSource inputSource, uint sourceId)
        {
            return inputSource.SupportsInputInfo(sourceId, SupportedInputInfo.PointingRay);
        }

        private void AttachInputSourcePointer(BaseInputEventData eventData)
        {
            AttachInputSourcePointer(eventData.InputSource, eventData.SourceId);
        }

        private void AttachInputSourcePointer(InputSourceInfo source)
        {
            AttachInputSourcePointer(source.InputSource, source.SourceId);
        }

        private void AttachInputSourcePointer(IInputSource inputSource, uint sourceId)
        {
            inputSourcePointer.InputSource = inputSource;
            inputSourcePointer.InputSourceId = sourceId;
            inputSourcePointer.RayStabilizer = ControllerPointerStabilizer;
            inputSourcePointer.OwnAllInput = true;
            inputSourcePointer.ExtentOverride = null;
            inputSourcePointer.PrioritizedLayerMasksOverride = null;
        }

        private bool IsInputSourcePointerActive
        {
            get { return (currentPointer == inputSourcePointer); }
        }

        private bool IsGazePointerActive
        {
            get { return object.ReferenceEquals(currentPointer, GazeManager.Instance); }
        }

        #endregion
    }
}
