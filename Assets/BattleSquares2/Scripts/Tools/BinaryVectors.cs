using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public sealed class BinaryVectors
{

    [Serializable]
    public struct SByte4
    {
        public Byte4 byteVec;
        public Vector4 min, max;
        public byte xBytes, yBytes, zBytes, wBytes;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFromVec4(in Vector4 data) => BinaryTool.CompressVector4PreAlloc(ref byteVec.data, data, xBytes, yBytes, zBytes, wBytes, min, max);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFromfloat4(in float4 data) => BinaryTool.CompressVector4PreAlloc(ref byteVec.data, new Vector4(data.x, data.y, data.z, data.w), xBytes, yBytes, zBytes, wBytes, min, max);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFromQuat(in Quaternion data) => BinaryTool.CompressVector4PreAlloc(ref byteVec.data, new Vector4(data.x, data.y, data.z, data.w), xBytes, yBytes, zBytes, wBytes, min, max);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFromByte4(in Byte4 data) => byteVec.data = data.data;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFromByteArr(in byte[] data) => byteVec.data = data;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 GetVec4() => BinaryTool.DecompressVector4(byteVec.data, xBytes, yBytes, zBytes, wBytes, min, max);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 Getfloat4() => BinaryTool.DecompressVector4(byteVec.data, xBytes, yBytes, zBytes, wBytes, min, max);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion GetQuat()
        {
            Vector4 vec = BinaryTool.DecompressVector4(byteVec.data, xBytes, yBytes, zBytes, wBytes, min, max);
            return new Quaternion(vec.x, vec.y, vec.z, vec.w);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion Getquat()
        {
            Vector4 vec = BinaryTool.DecompressVector4(byteVec.data, xBytes, yBytes, zBytes, wBytes, min, max);
            return new quaternion(vec.x, vec.y, vec.z, vec.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Byte4 GetByte4() => byteVec;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetXBytes(in byte bytes) => xBytes = bytes;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetYBytes(in byte bytes) => yBytes = bytes;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetZBytes(in byte bytes) => zBytes = bytes;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetWBytes(in byte bytes) => wBytes = bytes;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMin(in Vector4 minimum) => min = minimum;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMax(in Vector4 maximum) => max = maximum;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMin(in float4 minimum) => (min.x, min.y, min.z, min.w) = (minimum.x, minimum.y, minimum.z, minimum.w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMax(in float4 maximum) => (max.x, max.y, max.z, max.w) = (maximum.x, maximum.y, maximum.z, maximum.w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMin(in Quaternion minimum) => (min.x, min.y, min.z, min.w) = (minimum.x, minimum.y, minimum.z, minimum.w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMax(in Quaternion maximum) => (max.x, max.y, max.z, max.w) = (maximum.x, maximum.y, maximum.z, maximum.w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMin(in quaternion minimum) => (min.x, min.y, min.z, min.w) = (minimum.value.x, minimum.value.y, minimum.value.z, minimum.value.w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMax(in quaternion maximum) => (max.x, max.y, max.z, max.w) = (maximum.value.x, maximum.value.y, maximum.value.z, maximum.value.w);
    }

    [Serializable]
    public struct SByte3
    {
        public Byte3 byteVec;
        public Vector3 min, max;
        public byte xBytes, yBytes, zBytes;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFromVec3(in Vector3 data) => BinaryTool.CompressVector3PreAlloc(ref byteVec.data, data, xBytes, yBytes, zBytes, min, max);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFromfloat3(in float3 data) => BinaryTool.CompressVector3PreAlloc(ref byteVec.data, new Vector3(data.x, data.y, data.z), xBytes, yBytes, zBytes, min, max);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFromByte3(in Byte3 data) => byteVec.data = data.data;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFromByteArr(in byte[] data) => byteVec.data = data;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetVec3() => BinaryTool.DecompressVector3(byteVec.data, xBytes, yBytes, zBytes, min, max);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 Getfloat3() => BinaryTool.DecompressVector3(byteVec.data, xBytes, yBytes, zBytes, min, max);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Byte3 GetByte3() => byteVec;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetXBytes(in byte bytes) => xBytes = bytes;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetYBytes(in byte bytes) => yBytes = bytes;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetZBytes(in byte bytes) => zBytes = bytes;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMin(in Vector3 minimum) => min = minimum;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMax(in Vector3 maximum) => max = maximum;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMin(in float3 minimum) => (min.x, min.y, min.z) = (minimum.x, minimum.y, minimum.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMax(in float3 maximum) => (max.x, max.y, max.z) = (maximum.x, maximum.y, maximum.z);
    }


    [Serializable]
    public struct SByte2
    {

        public Byte2 byteVec;
        public Vector2 min, max;
        public byte xBytes, yBytes;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFromVec2(in Vector2 data) => BinaryTool.CompressVector2PreAlloc(ref byteVec.data, data, xBytes, yBytes, min, max);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFromfloat2(in float2 data) => BinaryTool.CompressVector2PreAlloc(ref byteVec.data, new Vector2(data.x, data.y), xBytes, yBytes, min, max);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFromByte2(in Byte2 data) => byteVec.data = data.data;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFromByteArr(in byte[] data) => byteVec.data = data;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 GetVec2() => BinaryTool.DecompressVector2(byteVec.data, xBytes, yBytes, min, max);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 Getfloat2() => BinaryTool.DecompressVector2(byteVec.data, xBytes, yBytes, min, max);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Byte2 GetByte2() => byteVec;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetXBytes(in byte bytes) => xBytes = bytes;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetYBytes(in byte bytes) => yBytes = bytes;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMin(in Vector2 minimum) => min = minimum;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMax(in Vector2 maximum) => max = maximum;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMin(in float2 minimum) => (min.x, min.y) = (minimum.x, minimum.y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMax(in float2 maximum) => (max.x, max.y) = (maximum.x, maximum.y);
    }
    [Serializable]
    public struct Byte2 : INetworkSerializable
    {
        public byte[] data;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter => s.SerializeValue(ref data);
    }

    [Serializable]
    public struct Byte4 : INetworkSerializable
    {
        public byte[] data;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter => s.SerializeValue(ref data);
    }

    [Serializable]
    public struct Byte3 : INetworkSerializable
    {
        public byte[] data;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter => s.SerializeValue(ref data);
    }


}
