using UnityEngine;
using System;
using System.IO;

public static class WavUtility
{
    const int HEADER_SIZE = 44;

    public static byte[] FromAudioClip(AudioClip clip, out string filepath, bool saveToFile = false)
    {
        var data = ConvertAndWrite(clip, out filepath);

        if (saveToFile)
        {
            File.WriteAllBytes(filepath, data);
        }

        return data;
    }

    public static byte[] FromAudioClip(AudioClip clip)
    {
        return ConvertAndWrite(clip, out _);
    }

    private static byte[] ConvertAndWrite(AudioClip clip, out string filepath)
    {
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        var fileName = $"recorded_clip_{DateTime.Now.Ticks}.wav";
        filepath = Path.Combine(Application.persistentDataPath, fileName);

        byte[] wav = ConvertAudioClipDataToInt16ByteArray(samples, clip.channels);
        byte[] header = WriteHeader(wav.Length, clip.channels, clip.frequency);

        byte[] final = new byte[header.Length + wav.Length];
        Buffer.BlockCopy(header, 0, final, 0, header.Length);
        Buffer.BlockCopy(wav, 0, final, header.Length, wav.Length);

        return final;
    }

    private static byte[] ConvertAudioClipDataToInt16ByteArray(float[] samples, int channels)
    {
        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];

        const float rescaleFactor = 32767;

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        return bytesData;
    }

    private static byte[] WriteHeader(int dataLength, int channels, int sampleRate)
    {
        byte[] header = new byte[HEADER_SIZE];

        int fileSize = HEADER_SIZE + dataLength - 8;
        short bitsPerSample = 16;
        short blockAlign = (short)(channels * bitsPerSample / 8);
        int byteRate = sampleRate * blockAlign;

        using (MemoryStream stream = new MemoryStream(header))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(fileSize);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(dataLength);
        }

        return header;
    }
}
