﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HoloToolkit.Unity.InputModule
{
    // TODO: robertes: comment for HoloToolkit release.
    public interface IPointingSource
    {
        Ray Ray { get; }
        float? ExtentOverride { get; }
        IList<LayerMask> PrioritizedLayerMasksOverride { get; }

        void UpdatePointer();

        bool OwnsInput(BaseEventData eventData);
    }
}
