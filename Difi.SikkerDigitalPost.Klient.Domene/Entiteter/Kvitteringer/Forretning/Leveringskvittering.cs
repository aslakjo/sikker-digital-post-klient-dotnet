﻿using System;

namespace Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Kvitteringer.Forretning
{
    /// <summary>
    /// En kvittering som Avsender kan oppbevare som garanti på at posten er levert til Mottaker. 
    /// Kvitteringen sendes fra Postkassleverandør når postforsendelsen er validert og de garanterer for at posten vil bli tilgjengeliggjort.
    /// Les mer på http://begrep.difi.no/SikkerDigitalPost/1.0.2/meldinger/LeveringsKvittering.
    /// </summary>
    public class Leveringskvittering : Forretningskvittering
    {
        public DateTime Levert {get { return Generert; } }

        public Leveringskvittering(Guid konversasjonsId, string bodyReferenceUri, string digestValue) : base(konversasjonsId, bodyReferenceUri, digestValue)
        {
        }

        public override string ToString()
        {
            return string.Format("{0} med meldingsId {1}: \nLevert: {2}. \nKonversasjonsId: {3}. \nRefererer til melding med id: {4}", 
                GetType().Name, MeldingsId, Levert, KonversasjonsId, ReferanseTilMeldingId);
        }
    }
}
