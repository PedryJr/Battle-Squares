using System;
using Unity.Netcode;
using UnityEngine;

public class BinaryVectors
{

    [Serializable]
    public struct SByte4
    {
        public byte xBytes, yBytes, zBytes, wBytes;
        public Vector4 min, max;
        public Byte4 byteVec;

        public void SetFromVec4(Vector4 data) => BinaryTool.CompressVector4(ref byteVec.data, data, xBytes, yBytes, zBytes, wBytes, min, max);
        public void SetFromByte4(Byte4 data) => byteVec.data = data.data;
        public void SetFromByteArr(byte[] data) => byteVec.data = data;
        public Vector4 GetVec4() => BinaryTool.DecompressVector4(byteVec.data, xBytes, yBytes, zBytes, wBytes, min, max);
        public Byte4 GetByte4() => byteVec;
    }

    [Serializable]
    public struct SByte3
    {
        public byte xBytes, yBytes, zBytes;
        public Vector3 min, max;
        public Byte3 byteVec;

        public void SetFromVec3(Vector3 data) => BinaryTool.CompressVector3(ref byteVec.data, data, xBytes, yBytes, zBytes, min, max);
        public void SetFromByte3(Byte3 data) => byteVec.data = data.data;
        public void SetFromByteArr(byte[] data) => byteVec.data = data;
        public Vector3 GetVec3() => BinaryTool.DecompressVector3(byteVec.data, xBytes, yBytes, zBytes, min, max);
        public Byte3 GetByte3() => byteVec;
    }


    [Serializable]
    public struct SByte2
    {
        public byte xBytes, yBytes;
        public Vector2 min, max;
        public Byte2 byteVec;

        public void SetFromVec2(Vector2 data) => BinaryTool.CompressVector2(ref byteVec.data, data, xBytes, yBytes, min, max);
        public void SetFromByte2(Byte2 data) => byteVec.data = data.data;
        public void SetFromByteArr(byte[] data) => byteVec.data = data;
        public Vector2 GetVec2() => BinaryTool.DecompressVector2(byteVec.data, xBytes, yBytes, min, max);
        public Byte2 GetByte2() => byteVec;
    }
    [Serializable]
    public struct Byte2 : INetworkSerializable
    {
        public byte[] data;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter => s.SerializeValue(ref data);
    }

    [Serializable]
    public struct Byte4 : INetworkSerializable
    {
        public byte[] data;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter => s.SerializeValue(ref data);
    }

    [Serializable]
    public struct Byte3 : INetworkSerializable
    {
        public byte[] data;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter => s.SerializeValue(ref data);
    }


}
