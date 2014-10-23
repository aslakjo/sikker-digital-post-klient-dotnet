﻿using System;
using System.Xml;
using SikkerDigitalPost.Domene.Exceptions;

namespace SikkerDigitalPost.Domene.Entiteter.Kvitteringer
{
    /// <summary>
    /// Abstrakt klasse for transportkvitteringer.
    /// </summary>
    public abstract class Transportkvittering
    {
        private readonly XmlDocument _document;
        private readonly XmlNamespaceManager _namespaceManager;

        /// <summary>
        /// Tidspunktet da kvitteringen ble sendt.
        /// </summary>
        public DateTime Tidspunkt { get; protected set; }

        /// <summary>
        /// Unik identifikator for kvitteringen.
        /// </summary>
        public string MeldingsId { get; protected set; }

        /// <summary>
        /// Refereranse til en annen relatert melding. Refererer til den relaterte meldingens MessageId.
        /// </summary>
        public string ReferanseTilMeldingsId { get; protected set; }

        /// <summary>
        /// Kvitteringen presentert som tekststreng.
        /// </summary>
        public string Rådata { get; protected set; }

        protected Transportkvittering(XmlDocument document, XmlNamespaceManager namespaceManager)
        {
            try
            {
                _document = document;
                _namespaceManager = namespaceManager;
                Tidspunkt = Convert.ToDateTime(DocumentNode("//ns6:Timestamp").InnerText);
                MeldingsId = DocumentNode("//ns6:MessageId").InnerText;
                Rådata = document.OuterXml;
            }
            catch (Exception e)
            {
                throw new XmlParseException(
                   String.Format("Feil under bygging av {0} (av type Transportkvittering). Klarte ikke finne alle felter i xml."
                   , GetType()), e);
            }
        }

        protected XmlNode DocumentNode(string xPath)
        {
            try
            {
                var rot = _document.DocumentElement;
                var targetNode = rot.SelectSingleNode(xPath, _namespaceManager);

                return targetNode;
            }
            catch (Exception e)
            {
                throw new XmlParseException(
                    String.Format("Feil under henting av dokumentnode i {0} (av type Transportkvittering). Klarte ikke finne alle felter i xml."
                    , GetType()), e);
            }
        }
    }
}