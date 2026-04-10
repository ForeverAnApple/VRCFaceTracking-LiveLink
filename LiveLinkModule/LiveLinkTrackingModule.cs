using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFaceTracking.Core.Types;

namespace LiveLinkModule;

public class LiveLinkTrackingModule : ExtTrackingModule
{
    private UdpClient? _udpClient;
    private IPEndPoint _remoteEndpoint = new(IPAddress.Any, 0);

    private const int Port = 11111;
    private const int BlendshapeCount = 61;
    private const int BlendshapeBytes = BlendshapeCount * 4; // 244 bytes

    public override (bool SupportsEye, bool SupportsExpression) Supported => (true, true);

    public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
    {
        ModuleInformation.Name = "LiveLink";

        _udpClient = new UdpClient(Port);
        _udpClient.Client.ReceiveTimeout = 1000;

        Logger.LogInformation("LiveLink module initialized, listening on UDP port {Port}", Port);
        return (true, true);
    }

    public override void Update()
    {
        float[]? v = ReadPacket();
        if (v == null)
            return;

        UpdateEyes(v);
        UpdateExpressions(v);
    }

    public override void Teardown()
    {
        _udpClient?.Close();
        _udpClient?.Dispose();
        Logger.LogInformation("LiveLink module teardown");
    }

    private float[]? ReadPacket()
    {
        try
        {
            byte[] data = _udpClient!.Receive(ref _remoteEndpoint);

            if (data.Length < BlendshapeBytes)
                return null;

            // The last 244 bytes contain 61 big-endian floats (ARKit blendshapes)
            int offset = data.Length - BlendshapeBytes;
            float[] values = new float[BlendshapeCount];

            for (int i = 0; i < BlendshapeCount; i++)
            {
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(data, offset + i * 4, 4);
                values[i] = BitConverter.ToSingle(data, offset + i * 4);
            }

            return values;
        }
        catch (SocketException)
        {
            return null;
        }
    }

    // ARKit blendshape indices (from LiveLink protocol, matching Apple ARKit order)
    // Eye
    private const int EyeBlinkLeft = 0;
    private const int EyeLookDownLeft = 1;
    private const int EyeLookInLeft = 2;
    private const int EyeLookOutLeft = 3;
    private const int EyeLookUpLeft = 4;
    private const int EyeSquintLeft = 5;
    private const int EyeWideLeft = 6;
    private const int EyeBlinkRight = 7;
    private const int EyeLookDownRight = 8;
    private const int EyeLookInRight = 9;
    private const int EyeLookOutRight = 10;
    private const int EyeLookUpRight = 11;
    private const int EyeSquintRight = 12;
    private const int EyeWideRight = 13;
    // Jaw / Mouth
    private const int JawForward = 14;
    private const int JawLeft = 15;
    private const int JawRight = 16;
    private const int JawOpen = 17;
    private const int MouthClose = 18;
    private const int MouthFunnel = 19;
    private const int MouthPucker = 20;
    private const int MouthLeft = 21;
    private const int MouthRight = 22;
    private const int MouthSmileLeft = 23;
    private const int MouthSmileRight = 24;
    private const int MouthFrownLeft = 25;
    private const int MouthFrownRight = 26;
    private const int MouthDimpleLeft = 27;
    private const int MouthDimpleRight = 28;
    private const int MouthStretchLeft = 29;
    private const int MouthStretchRight = 30;
    private const int MouthRollLower = 31;
    private const int MouthRollUpper = 32;
    private const int MouthShrugLower = 33;
    private const int MouthShrugUpper = 34;
    private const int MouthPressLeft = 35;
    private const int MouthPressRight = 36;
    private const int MouthLowerDownLeft = 37;
    private const int MouthLowerDownRight = 38;
    private const int MouthUpperUpLeft = 39;
    private const int MouthUpperUpRight = 40;
    // Brow
    private const int BrowDownLeft = 41;
    private const int BrowDownRight = 42;
    private const int BrowInnerUp = 43;
    private const int BrowOuterUpLeft = 44;
    private const int BrowOuterUpRight = 45;
    // Cheek / Nose / Tongue
    private const int CheekPuff = 46;
    private const int CheekSquintLeft = 47;
    private const int CheekSquintRight = 48;
    private const int NoseSneerLeft = 49;
    private const int NoseSneerRight = 50;
    private const int TongueOut = 51;
    // Head pose
    private const int HeadYaw = 52;
    private const int HeadPitch = 53;
    private const int HeadRoll = 54;
    // Eye gaze (dedicated yaw/pitch/roll per eye)
    private const int EyeYawLeft = 55;
    private const int EyePitchLeft = 56;
    private const int EyeRollLeft = 57;
    private const int EyeYawRight = 58;
    private const int EyePitchRight = 59;
    private const int EyeRollRight = 60;

    private static void UpdateEyes(float[] v)
    {
        ref var eye = ref UnifiedTracking.Data.Eye;

        eye.Left.Gaze = new Vector2(v[EyeYawLeft], -v[EyePitchLeft]);
        eye.Right.Gaze = new Vector2(v[EyeYawRight], -v[EyePitchRight]);

        eye.Left.Openness = Math.Clamp(1.0f - v[EyeBlinkLeft], 0f, 1f);
        eye.Right.Openness = Math.Clamp(1.0f - v[EyeBlinkRight], 0f, 1f);

        eye.Left.PupilDiameter_MM = 5f;
        eye.Right.PupilDiameter_MM = 5f;
        eye._minDilation = 0;
        eye._maxDilation = 10;
    }

    private static void UpdateExpressions(float[] v)
    {
        var s = UnifiedTracking.Data.Shapes;

        // Eye expressions
        s[(int)UnifiedExpressions.EyeSquintLeft].Weight = v[EyeSquintLeft];
        s[(int)UnifiedExpressions.EyeSquintRight].Weight = v[EyeSquintRight];
        s[(int)UnifiedExpressions.EyeWideLeft].Weight = v[EyeWideLeft];
        s[(int)UnifiedExpressions.EyeWideRight].Weight = v[EyeWideRight];

        // Brow expressions
        s[(int)UnifiedExpressions.BrowLowererLeft].Weight = v[BrowDownLeft];
        s[(int)UnifiedExpressions.BrowLowererRight].Weight = v[BrowDownRight];
        s[(int)UnifiedExpressions.BrowPinchLeft].Weight = v[BrowDownLeft];
        s[(int)UnifiedExpressions.BrowPinchRight].Weight = v[BrowDownRight];
        s[(int)UnifiedExpressions.BrowInnerUpLeft].Weight = v[BrowInnerUp];
        s[(int)UnifiedExpressions.BrowInnerUpRight].Weight = v[BrowInnerUp];
        s[(int)UnifiedExpressions.BrowOuterUpLeft].Weight = v[BrowOuterUpLeft];
        s[(int)UnifiedExpressions.BrowOuterUpRight].Weight = v[BrowOuterUpRight];

        // Jaw expressions
        s[(int)UnifiedExpressions.JawOpen].Weight = v[JawOpen];
        s[(int)UnifiedExpressions.JawLeft].Weight = v[JawLeft];
        s[(int)UnifiedExpressions.JawRight].Weight = v[JawRight];
        s[(int)UnifiedExpressions.JawForward].Weight = v[JawForward];
        s[(int)UnifiedExpressions.MouthClosed].Weight = v[MouthClose];

        // Cheek expressions
        s[(int)UnifiedExpressions.CheekPuffLeft].Weight = v[CheekPuff];
        s[(int)UnifiedExpressions.CheekPuffRight].Weight = v[CheekPuff];
        s[(int)UnifiedExpressions.CheekSquintLeft].Weight = v[CheekSquintLeft];
        s[(int)UnifiedExpressions.CheekSquintRight].Weight = v[CheekSquintRight];

        // Nose
        s[(int)UnifiedExpressions.NoseSneerLeft].Weight = v[NoseSneerLeft];
        s[(int)UnifiedExpressions.NoseSneerRight].Weight = v[NoseSneerRight];

        // Mouth - smile / frown
        s[(int)UnifiedExpressions.MouthCornerPullLeft].Weight = v[MouthSmileLeft];
        s[(int)UnifiedExpressions.MouthCornerPullRight].Weight = v[MouthSmileRight];
        s[(int)UnifiedExpressions.MouthCornerSlantLeft].Weight = v[MouthSmileLeft];
        s[(int)UnifiedExpressions.MouthCornerSlantRight].Weight = v[MouthSmileRight];
        s[(int)UnifiedExpressions.MouthFrownLeft].Weight = v[MouthFrownLeft];
        s[(int)UnifiedExpressions.MouthFrownRight].Weight = v[MouthFrownRight];

        // Mouth - direction
        s[(int)UnifiedExpressions.MouthUpperLeft].Weight = v[MouthLeft];
        s[(int)UnifiedExpressions.MouthUpperRight].Weight = v[MouthRight];
        s[(int)UnifiedExpressions.MouthLowerLeft].Weight = v[MouthLeft];
        s[(int)UnifiedExpressions.MouthLowerRight].Weight = v[MouthRight];

        // Mouth - upper lip raise / lower lip depress
        s[(int)UnifiedExpressions.MouthUpperUpLeft].Weight = v[MouthUpperUpLeft];
        s[(int)UnifiedExpressions.MouthUpperUpRight].Weight = v[MouthUpperUpRight];
        s[(int)UnifiedExpressions.MouthLowerDownLeft].Weight = v[MouthLowerDownLeft];
        s[(int)UnifiedExpressions.MouthLowerDownRight].Weight = v[MouthLowerDownRight];

        // Mouth - stretch / dimple / press
        s[(int)UnifiedExpressions.MouthStretchLeft].Weight = v[MouthStretchLeft];
        s[(int)UnifiedExpressions.MouthStretchRight].Weight = v[MouthStretchRight];
        s[(int)UnifiedExpressions.MouthDimpleLeft].Weight = v[MouthDimpleLeft];
        s[(int)UnifiedExpressions.MouthDimpleRight].Weight = v[MouthDimpleRight];
        s[(int)UnifiedExpressions.MouthPressLeft].Weight = v[MouthPressLeft];
        s[(int)UnifiedExpressions.MouthPressRight].Weight = v[MouthPressRight];

        // Mouth - lip funnel / pucker / suck / roll
        s[(int)UnifiedExpressions.LipFunnelUpperLeft].Weight = v[MouthFunnel];
        s[(int)UnifiedExpressions.LipFunnelUpperRight].Weight = v[MouthFunnel];
        s[(int)UnifiedExpressions.LipFunnelLowerLeft].Weight = v[MouthFunnel];
        s[(int)UnifiedExpressions.LipFunnelLowerRight].Weight = v[MouthFunnel];
        s[(int)UnifiedExpressions.LipPuckerUpperLeft].Weight = v[MouthPucker];
        s[(int)UnifiedExpressions.LipPuckerUpperRight].Weight = v[MouthPucker];
        s[(int)UnifiedExpressions.LipPuckerLowerLeft].Weight = v[MouthPucker];
        s[(int)UnifiedExpressions.LipPuckerLowerRight].Weight = v[MouthPucker];
        s[(int)UnifiedExpressions.LipSuckUpperLeft].Weight = v[MouthRollUpper];
        s[(int)UnifiedExpressions.LipSuckUpperRight].Weight = v[MouthRollUpper];
        s[(int)UnifiedExpressions.LipSuckLowerLeft].Weight = v[MouthRollLower];
        s[(int)UnifiedExpressions.LipSuckLowerRight].Weight = v[MouthRollLower];

        // Mouth - shrug maps to MouthRaiser
        s[(int)UnifiedExpressions.MouthRaiserUpper].Weight = v[MouthShrugUpper];
        s[(int)UnifiedExpressions.MouthRaiserLower].Weight = v[MouthShrugLower];

        // Tongue
        s[(int)UnifiedExpressions.TongueOut].Weight = v[TongueOut];
    }
}
