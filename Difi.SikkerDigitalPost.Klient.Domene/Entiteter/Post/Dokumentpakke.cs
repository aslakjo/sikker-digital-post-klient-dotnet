﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Difi.SikkerDigitalPost.Klient.Domene.Exceptions;

namespace Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Post
{
    public class Dokumentpakke
    {
        public Dokument Hoveddokument { get; }

        private List<Dokument> _vedlegg;
        public IReadOnlyList<Dokument> Vedlegg
        {
            get { return new ReadOnlyCollection<Dokument>(_vedlegg); }
        }

        /// <param name="hoveddokument">Dokumentpakkens hoveddokument</param>
        public Dokumentpakke(Dokument hoveddokument)
        {
            if (hoveddokument.Bytes.Length == 0)
            {
                throw new KonfigurasjonsException("Du prøver å legge til et hoveddokument som er tomt. Dette er ikke tillatt.");
            }
            _vedlegg = new List<Dokument>();
            Hoveddokument = hoveddokument;
            Hoveddokument.Id = "Id_2";
        }

        /// <summary>
        /// Legger til vedlegg i tillegg til allerede eksisterende vedlegg.
        /// </summary>
        /// <param name="dokumenter"></param>
        public void LeggTilVedlegg(params Dokument[] dokumenter)
        {
            LeggTilVedlegg(dokumenter.ToList());
        }

        /// <summary>
        /// Legger til vedlegg i tillegg til allerede eksisterende vedlegg.
        /// </summary>
        /// <param name="dokumenter"></param>
        public void LeggTilVedlegg(IEnumerable<Dokument> dokumenter)
        {
            int startId = Vedlegg.Count();
            for (int i = 0; i < dokumenter.Count(); i++)
            {
                var dokument = dokumenter.ElementAt(i);

                var likeFiler = Vedlegg.Any(v => dokument.Filnavn == v.Filnavn) || dokument.Filnavn == Hoveddokument.Filnavn;
                
                if (likeFiler)
                {
                    throw new KonfigurasjonsException(string.Format("Dokumentet {0} som du prøver å legge til, eksisterer allerede med samme filnavn. Det er ikke tillatt å ha likt filnavn på to vedlegg, eller vedlegg med samme filnavn som hoveddokument.", dokument.Filnavn));
                }

                if (dokument.Bytes.Length == 0)
                    throw new KonfigurasjonsException(string.Format("Dokumentet {0} som du prøver å legge til som vedlegg er tomt. Det er ikke tillatt å sende tomme dokumenter.", dokument.Filnavn));
                dokument.Id = string.Format("Id_{0}", i + 3 + startId);
                _vedlegg.Add(dokument);
            }
        }
    }
}
