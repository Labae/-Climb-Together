// Copyright (c) Team STEP.  All Rights Reserved.

namespace Systems.Input.Interfaces
{
    public interface IGlobalInputSystem
    {
        InputSystemActions Actions { get; }
        float InputBuffer { get; }
    }
}
