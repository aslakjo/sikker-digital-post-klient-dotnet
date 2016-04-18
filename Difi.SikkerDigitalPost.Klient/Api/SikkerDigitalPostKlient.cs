﻿using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Difi.SikkerDigitalPost.Klient.AsicE;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Aktører;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Kvitteringer;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Kvitteringer.Forretning;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Kvitteringer.Transport;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Post;
using Difi.SikkerDigitalPost.Klient.Domene.Exceptions;
using Difi.SikkerDigitalPost.Klient.Envelope;
using Difi.SikkerDigitalPost.Klient.Envelope.Forretningsmelding;
using Difi.SikkerDigitalPost.Klient.Envelope.Kvitteringsbekreftelse;
using Difi.SikkerDigitalPost.Klient.Envelope.Kvitteringsforespørsel;
using Difi.SikkerDigitalPost.Klient.Internal;
using Difi.SikkerDigitalPost.Klient.Utilities;
using Difi.SikkerDigitalPost.Klient.XmlValidering;

namespace Difi.SikkerDigitalPost.Klient.Api
{
    public class SikkerDigitalPostKlient : ISikkerDigitalPostKlient
    {
        public Databehandler Databehandler { get; }

        public Klientkonfigurasjon Klientkonfigurasjon { get; }

        internal RequestHelper RequestHelper { get; set; }

        /// <param name="databehandler">
        ///     Virksomhet (offentlig eller privat) som har en kontraktfestet avtale med Avsender med
        ///     formål å dekke hele eller deler av prosessen med å formidle en digital postmelding fra
        ///     Behandlingsansvarlig til Meldingsformidler. Det kan være flere databehandlere som har
        ///     ansvar for forskjellige steg i prosessen med å formidle en digital postmelding.
        /// </param>
        /// <param name="klientkonfigurasjon">
        ///     Klientkonfigurasjon for klienten. Brukes for å sette parametere
        ///     som proxy, timeout og URI til meldingsformidler. For å bruke standardkonfigurasjon, lag
        ///     SikkerDigitalPostKlient uten Klientkonfigurasjon som parameter.
        /// </param>
        /// <remarks>
        ///     Se <a href="http://begrep.difi.no/SikkerDigitalPost/forretningslag/Aktorer">oversikt over aktører</a>
        /// </remarks>
        public SikkerDigitalPostKlient(Databehandler databehandler, Klientkonfigurasjon klientkonfigurasjon)
        {
            Databehandler = databehandler;
            Klientkonfigurasjon = klientkonfigurasjon;
            RequestHelper = new RequestHelper(klientkonfigurasjon);
            
            Logging.Initialize(klientkonfigurasjon);
            FileUtility.BasePath = klientkonfigurasjon.StandardLoggSti;
        }

        /// <summary>
        ///     Sender en forsendelse til meldingsformidler. Dersom noe feilet i sendingen til meldingsformidler, vil det kastes en
        ///     exception.
        /// </summary>
        /// <param name="forsendelse">
        ///     Et objekt som har all informasjon klar til å kunne sendes (mottakerinformasjon, sertifikater,
        ///     vedlegg mm), enten digitalt eller fysisk.
        /// </param>
        /// <param name="lagreDokumentpakke">Hvis satt til true, så lagres dokumentpakken på Klientkonfigurasjon.StandardLoggSti.</param>
        public Transportkvittering Send(Forsendelse forsendelse, bool lagreDokumentpakke = false)
        {
            return SendAsync(forsendelse, lagreDokumentpakke).Result;
        }

        /// <summary>
        ///     Sender en forsendelse til meldingsformidler. Dersom noe feilet i sendingen til meldingsformidler, vil det kastes en
        ///     exception.
        /// </summary>
        /// <param name="forsendelse">
        ///     Et objekt som har all informasjon klar til å kunne sendes (mottakerinformasjon, sertifikater,
        ///     vedlegg mm), enten digitalt eller fysisk.
        /// </param>
        /// <param name="lagreDokumentpakke">Hvis satt til true, så lagres dokumentpakken på Klientkonfigurasjon.StandardLoggSti.</param>
        public async Task<Transportkvittering> SendAsync(Forsendelse forsendelse, bool lagreDokumentpakke = false)
        {
            Logging.Log(TraceEventType.Information, forsendelse.KonversasjonsId, "Sender ny forsendelse til meldingsformidler.");

            var guidHandler = new GuidUtility();

           var asicEArkiv = LagAsicEArkiv(forsendelse, lagreDokumentpakke, guidHandler);
            var forretningsmeldingEnvelope = LagForretningsmeldingEnvelope(forsendelse, asicEArkiv, guidHandler);

            Logg(TraceEventType.Verbose, forsendelse.KonversasjonsId, asicEArkiv.Signatur.Xml().OuterXml, true, true, "Sendt - Signatur.xml");
            Logg(TraceEventType.Verbose, forsendelse.KonversasjonsId, asicEArkiv.Manifest.Xml().OuterXml, true, true, "Sendt - Manifest.xml");
            Logg(TraceEventType.Verbose, forsendelse.KonversasjonsId, forretningsmeldingEnvelope.Xml().OuterXml, true, true, "Sendt - Envelope.xml");

            try
            {
                ValiderForretningsmeldingEnvelope(forretningsmeldingEnvelope.Xml());
                ValiderArkivManifest(asicEArkiv.Manifest.Xml());
                ValiderArkivSignatur(asicEArkiv.Signatur.Xml());
            }
            catch (Exception e)
            {
                throw new Exception("Sending av forsendelse feilet under validering. Feilmelding: " + e.GetBaseException(), e.InnerException);
            }
            
            var soapContainer = new SoapContainer(forretningsmeldingEnvelope);
            soapContainer.Vedlegg.Add(asicEArkiv);
            
            var transportReceipt = (Transportkvittering) await RequestHelper.Send(soapContainer);
            transportReceipt.AntallBytesDokumentpakke = asicEArkiv.ContentBytesCount;
            var transportReceiptXml = XmlUtility.TilXmlDokument(transportReceipt.Rådata);

            Logging.Log(TraceEventType.Information, forsendelse.KonversasjonsId, "Kvittering for forsendelse" + Environment.NewLine + transportReceipt);

            if (transportReceipt is TransportOkKvittering)
            {
                SikkerhetsvalideringAvTransportkvittering(transportReceiptXml, forretningsmeldingEnvelope.Xml(), guidHandler);
            }

            return transportReceipt;
        }

        /// <summary>
        ///     Forespør kvittering for forsendelser. Kvitteringer blir tilgjengeliggjort etterhvert som de er klare i
        ///     meldingsformidler.
        ///     Det er ikke mulig å etterspørre kvittering for en spesifikk forsendelse.
        /// </summary>
        /// <param name="kvitteringsforespørsel"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <list type="table">
        ///         <listheader>
        ///             <description>
        ///                 Dersom det ikke er tilgjengelige kvitteringer skal det ventes følgende tidsintervaller før en
        ///                 ny forespørsel gjøres
        ///             </description>
        ///         </listheader>
        ///         <item>
        ///             <term>normal</term><description>Minimum 10 minutter</description>
        ///         </item>
        ///         <item>
        ///             <term>prioritert</term><description>Minimum 1 minutt</description>
        ///         </item>
        ///     </list>
        /// </remarks>
        public Kvittering HentKvittering(Kvitteringsforespørsel kvitteringsforespørsel)
        {
            return HentKvitteringOgBekreftForrige(kvitteringsforespørsel, null);
        }

        /// <summary>
        ///     Forespør kvittering for forsendelser. Kvitteringer blir tilgjengeliggjort etterhvert som de er klare i
        ///     meldingsformidler.
        ///     Det er ikke mulig å etterspørre kvittering for en spesifikk forsendelse.
        /// </summary>
        /// <param name="kvitteringsforespørsel"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <list type="table">
        ///         <listheader>
        ///             <description>
        ///                 Dersom det ikke er tilgjengelige kvitteringer skal det ventes følgende tidsintervaller før en
        ///                 ny forespørsel gjøres
        ///             </description>
        ///         </listheader>
        ///         <item>
        ///             <term>normal</term><description>Minimum 10 minutter</description>
        ///         </item>
        ///         <item>
        ///             <term>prioritert</term><description>Minimum 1 minutt</description>
        ///         </item>
        ///     </list>
        /// </remarks>
        public async Task<Kvittering> HentKvitteringAsync(Kvitteringsforespørsel kvitteringsforespørsel)
        {
            return await HentKvitteringOgBekreftForrigeAsync(kvitteringsforespørsel, null);
        }

        /// <summary>
        ///     Forespør kvittering for forsendelser med mulighet til å samtidig bekrefte på forrige kvittering for å slippe å
        ///     kjøre eget kall for bekreft.
        ///     Kvitteringer blir tilgjengeliggjort etterhvert som de er klare i meldingsformidler. Det er ikke mulig å etterspørre
        ///     kvittering for en
        ///     spesifikk forsendelse.
        /// </summary>
        /// <param name="kvitteringsforespørsel"></param>
        /// <param name="forrigeKvittering"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <list type="table">
        ///         <listheader>
        ///             <description>
        ///                 Dersom det ikke er tilgjengelige kvitteringer skal det ventes følgende tidsintervaller før en
        ///                 ny forespørsel gjøres
        ///             </description>
        ///         </listheader>
        ///         <item>
        ///             <term>normal</term><description>Minimum 10 minutter</description>
        ///         </item>
        ///         <item>
        ///             <term>prioritert</term><description>Minimum 1 minutt</description>
        ///         </item>
        ///     </list>
        /// </remarks>
        public Kvittering HentKvitteringOgBekreftForrige(Kvitteringsforespørsel kvitteringsforespørsel,
            Forretningskvittering forrigeKvittering)
        {
            return HentKvitteringOgBekreftForrigeAsync(kvitteringsforespørsel, forrigeKvittering).Result;
        }

        /// <summary>
        ///     Forespør kvittering for forsendelser med mulighet til å samtidig bekrefte på forrige kvittering for å slippe å
        ///     kjøre eget kall for bekreft.
        ///     Kvitteringer blir tilgjengeliggjort etterhvert som de er klare i meldingsformidler. Det er ikke mulig å etterspørre
        ///     kvittering for en
        ///     spesifikk forsendelse.
        /// </summary>
        /// <param name="kvitteringsforespørsel"></param>
        /// <param name="forrigeKvittering"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <list type="table">
        ///         <listheader>
        ///             <description>
        ///                 Dersom det ikke er tilgjengelige kvitteringer skal det ventes følgende tidsintervaller før en
        ///                 ny forespørsel gjøres
        ///             </description>
        ///         </listheader>
        ///         <item>
        ///             <term>normal</term><description>Minimum 10 minutter</description>
        ///         </item>
        ///         <item>
        ///             <term>prioritert</term><description>Minimum 1 minutt</description>
        ///         </item>
        ///     </list>
        /// </remarks>
        public async Task<Kvittering> HentKvitteringOgBekreftForrigeAsync(Kvitteringsforespørsel kvitteringsforespørsel, Forretningskvittering forrigeKvittering)
        {
            if (forrigeKvittering != null)
            {
                Bekreft(forrigeKvittering);
            }

            Logging.Log(TraceEventType.Information, "Henter kvittering for " + kvitteringsforespørsel.Mpc);

            var guidUtility = new GuidUtility();
            var envelopeSettings = new EnvelopeSettings(kvitteringsforespørsel, Databehandler, guidUtility);
            var kvitteringsforespørselEnvelope = new KvitteringsforespørselEnvelope(envelopeSettings);

            Logging.Log(TraceEventType.Verbose, "Envelope for kvitteringsforespørsel" + Environment.NewLine + kvitteringsforespørselEnvelope.Xml().OuterXml);

            ValiderKvitteringsEnvelope(kvitteringsforespørselEnvelope);

            var soapContainer = new SoapContainer(kvitteringsforespørselEnvelope);
            var transportReceipt = await RequestHelper.Send(soapContainer);

            Logg(TraceEventType.Verbose, Guid.Empty, kvitteringsforespørselEnvelope.Xml().OuterXml, true, true, "Sendt - Kvitteringsenvelope.xml");

            var transportReceiptXml = XmlUtility.TilXmlDokument(transportReceipt.Rådata);

            if (transportReceipt is TomKøKvittering)
            {
                SikkerhetsvalideringAvTomKøKvittering(transportReceiptXml, kvitteringsforespørselEnvelope.Xml());
            }
            else if (transportReceipt is Forretningskvittering)
            {
                SikkerhetsvalideringAvMeldingskvittering(transportReceiptXml, kvitteringsforespørselEnvelope);
            }

            return transportReceipt;
        }

        /// <summary>
        ///     Bekreft mottak av forretningskvittering gjennom <see cref="HentKvittering(Kvitteringsforespørsel)" />.
        ///     <list type="bullet">
        ///         <listheader>
        ///             <description>
        ///                 <para>Dette legger opp til følgende arbeidsflyt</para>
        ///             </description>
        ///         </listheader>
        ///         <item>
        ///             <description>
        ///                 <para>
        ///                     <see cref="HentKvittering(Kvitteringsforespørsel)" />
        ///                 </para>
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <para>Gjør intern prosessering av kvitteringen (lagre til database, og så videre)</para>
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <para>Bekreft mottak av kvittering</para>
        ///             </description>
        ///         </item>
        ///     </list>
        /// </summary>
        /// <param name="kvittering"></param>
        /// <remarks>
        ///     <see cref="HentKvittering(Kvitteringsforespørsel)" /> kommer ikke til å returnere en ny kvittering før mottak av
        ///     den forrige er bekreftet.
        /// </remarks>
        public void Bekreft(Forretningskvittering kvittering)
        {
            BekreftAsync(kvittering).Wait();
        }

        /// <summary>
        ///     Bekreft mottak av forretningskvittering gjennom <see cref="HentKvittering(Kvitteringsforespørsel)" />.
        ///     <list type="bullet">
        ///         <listheader>
        ///             <description>
        ///                 <para>Dette legger opp til følgende arbeidsflyt</para>
        ///             </description>
        ///         </listheader>
        ///         <item>
        ///             <description>
        ///                 <para>
        ///                     <see cref="HentKvittering(Kvitteringsforespørsel)" />
        ///                 </para>
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <para>Gjør intern prosessering av kvitteringen (lagre til database, og så videre)</para>
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <para>Bekreft mottak av kvittering</para>
        ///             </description>
        ///         </item>
        ///     </list>
        /// </summary>
        /// <param name="kvittering"></param>
        /// <remarks>
        ///     <see cref="HentKvittering(Kvitteringsforespørsel)" /> kommer ikke til å returnere en ny kvittering før mottak av
        ///     den forrige er bekreftet.
        /// </remarks>
        public async Task BekreftAsync(Forretningskvittering kvittering)
        {
            Logging.Log(TraceEventType.Information, kvittering.KonversasjonsId, "Bekrefter kvittering.");

            var envelopeSettings = new EnvelopeSettings(kvittering, Databehandler, new GuidUtility());
            var bekreftKvitteringEnvelope = new KvitteringsbekreftelseEnvelope(envelopeSettings);

            var filnavn = kvittering.GetType().Name + ".xml";
            Logg(TraceEventType.Verbose, kvittering.KonversasjonsId, kvittering, true, true, filnavn);

            try
            {
                var kvitteringMottattEnvelopeValidering = new KvitteringMottattEnvelopeValidator();
                var kvitteringMottattEnvelopeValidert = kvitteringMottattEnvelopeValidering.ValiderDokumentMotXsd(bekreftKvitteringEnvelope.Xml().OuterXml);
                if (!kvitteringMottattEnvelopeValidert)
                    throw new Exception(kvitteringMottattEnvelopeValidering.ValideringsVarsler);
            }
            catch (Exception e)
            {
                throw new XmlValidationException("Kvitteringsbekreftelse validerer ikke:" + e.Message);
            }

            var soapContainer = new SoapContainer(bekreftKvitteringEnvelope);
            await RequestHelper.Send(soapContainer);
        }

        private AsicEArkiv LagAsicEArkiv(Forsendelse forsendelse, bool lagreDokumentpakke, GuidUtility guidHandler)
        {
            var arkiv = new AsicEArkiv(forsendelse, guidHandler, Databehandler.Sertifikat);
            if (lagreDokumentpakke)
            {
                arkiv.LagreTilDisk(Klientkonfigurasjon.StandardLoggSti, "dokumentpakke",
                    DateUtility.DateForFile() + " - Dokumentpakke.zip");
            }
            return arkiv;
        }

        private ForretningsmeldingEnvelope LagForretningsmeldingEnvelope(Forsendelse forsendelse, AsicEArkiv arkiv,
            GuidUtility guidHandler)
        {
            var forretningsmeldingEnvelope =
                new ForretningsmeldingEnvelope(new EnvelopeSettings(forsendelse, arkiv, Databehandler, guidHandler,
                    Klientkonfigurasjon));
            return forretningsmeldingEnvelope;
        }

        private static void ValiderKvitteringsEnvelope(KvitteringsforespørselEnvelope kvitteringsenvelope)
        {
            try
            {
                var kvitteringForespørselEnvelopeValidering = new KvitteringsforespørselEnvelopeValidator();
                var kvitteringForespørselEnvelopeValidert =
                    kvitteringForespørselEnvelopeValidering.ValiderDokumentMotXsd(kvitteringsenvelope.Xml().OuterXml);
                if (!kvitteringForespørselEnvelopeValidert)
                    throw new Exception(kvitteringForespørselEnvelopeValidering.ValideringsVarsler);
            }
            catch (Exception e)
            {
                throw new XmlValidationException("Kvitteringsforespørsel validerer ikke mot xsd:" + e.Message);
            }
        }

        private void SikkerhetsvalideringAvTransportkvittering(XmlDocument kvittering, XmlDocument forretningsmelding, GuidUtility guidUtility)
        {
            var responsvalidator = new Responsvalidator(forretningsmelding, kvittering, Klientkonfigurasjon.Miljø.Sertifikatkjedevalidator);
            responsvalidator.ValiderTransportkvittering(guidUtility);
        }

        private void SikkerhetsvalideringAvTomKøKvittering(XmlDocument kvittering, XmlDocument forretningsmelding)
        {
            var responsvalidator = new Responsvalidator(forretningsmelding, kvittering, Klientkonfigurasjon.Miljø.Sertifikatkjedevalidator);
            responsvalidator.ValiderTomKøKvittering();
        }

        private void SikkerhetsvalideringAvMeldingskvittering(XmlDocument kvittering, KvitteringsforespørselEnvelope kvitteringsforespørselEnvelope)
        {
            var valideringAvResponsSignatur = new Responsvalidator(kvitteringsforespørselEnvelope.Xml(), kvittering, Klientkonfigurasjon.Miljø.Sertifikatkjedevalidator);
            valideringAvResponsSignatur.ValiderMeldingskvittering();
        }

        private static void ValiderForretningsmeldingEnvelope(XmlDocument forretningsmeldingEnvelopeXml)
        {
            const string preMessage = "Envelope validerer ikke: ";

            var envelopeValidering = new ForretningsmeldingEnvelopeValidator();
            var envelopeValidert = envelopeValidering.ValiderDokumentMotXsd(forretningsmeldingEnvelopeXml.OuterXml);
            if (!envelopeValidert)
                throw new XmlValidationException(preMessage + envelopeValidering.ValideringsVarsler);
        }

        private static void ValiderArkivSignatur(XmlDocument signaturXml)
        {
            const string preMessage = "Envelope validerer ikke: ";

            var signaturValidering = new Signaturvalidator();
            var signaturValidert = signaturValidering.ValiderDokumentMotXsd(signaturXml.OuterXml);
            if (!signaturValidert)
                throw new XmlValidationException(preMessage + signaturValidering.ValideringsVarsler);
        }

        private static void ValiderArkivManifest(XmlDocument manifestXml)
        {
            const string preMessage = "Envelope validerer ikke: ";

            var manifestValidering = new ManifestValidator();
            var manifestValidert = manifestValidering.ValiderDokumentMotXsd(manifestXml.OuterXml);
            if (!manifestValidert)
                throw new XmlValidationException(preMessage + manifestValidering.ValideringsVarsler);
        }

        private void Logg(TraceEventType viktighet, Guid konversasjonsId, string melding, bool datoPrefiks, bool isXml, string filnavn, params string[] filsti)
        {
            var fullFilsti = new string[filsti.Length + 1];
            for (var i = 0; i < filsti.Length; i ++)
            {
                var sti = filsti[i];
                fullFilsti[i] = sti;
            }

            filnavn = datoPrefiks ? $"{DateUtility.DateForFile()} - {filnavn}" : filnavn;
            fullFilsti[filsti.Length] = filnavn;

            if (Klientkonfigurasjon.LoggXmlTilFil && filnavn != null)
            {
                if (isXml)
                    FileUtility.WriteXmlToBasePath(melding, "logg", filnavn);
                else
                    FileUtility.WriteToBasePath(melding, "logg", filnavn);
            }

            Logging.Log(viktighet, konversasjonsId, melding);
        }

        private void Logg(TraceEventType viktighet, Guid konversasjonsId, byte[] melding, bool datoPrefiks, bool isXml, string filnavn, params string[] filsti)
        {
            var data = Encoding.UTF8.GetString(melding);
            Logg(viktighet, konversasjonsId, data, datoPrefiks, isXml, filnavn, filsti);
        }

        private void Logg(TraceEventType viktighet, Guid konversasjonsId, Forretningskvittering kvittering, bool datoPrefiks, bool isXml, params string[] filsti)
        {
            var fileSuffix = isXml ? ".xml" : ".txt";
            Logg(viktighet, konversasjonsId, kvittering.Rådata, datoPrefiks, isXml, "Mottatt - " + kvittering.GetType().Name + fileSuffix);
        }
    }
}