﻿using SikkerDigitalPost.Net.Domene.Entiteter.Kvitteringer;

namespace SikkerDigitalPost.Net.Domene.Entiteter
{
    public class StandardForretningsDokument
    {
        public string KonversasjonsId { get; set; }
        public Forretningskvittering Kvittering { get; set; }
    }
}
