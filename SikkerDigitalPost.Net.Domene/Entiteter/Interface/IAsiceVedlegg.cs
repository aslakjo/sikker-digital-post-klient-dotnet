﻿namespace SikkerDigitalPost.Net.Domene.Entiteter.Interface
{
    internal interface IAsiceVedlegg
    {
        string Filnavn { get; }
        byte[] Bytes { get; }
        string Innholdstype { get; }
    }
}
