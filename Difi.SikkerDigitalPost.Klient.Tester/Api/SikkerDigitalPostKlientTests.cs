﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Difi.SikkerDigitalPost.Klient.Api;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Aktører;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Kvitteringer.Transport;
using Difi.SikkerDigitalPost.Klient.Domene.Exceptions;
using Difi.SikkerDigitalPost.Klient.Internal;
using Difi.SikkerDigitalPost.Klient.Internal.AsicE;
using Difi.SikkerDigitalPost.Klient.Resources.Xml;
using Difi.SikkerDigitalPost.Klient.Tester.AsicE;
using Difi.SikkerDigitalPost.Klient.Tester.Fakes;
using Difi.SikkerDigitalPost.Klient.Tester.Utilities;
using Difi.SikkerDigitalPost.Klient.XmlValidering;
using Xunit;
using SecurityException = Difi.SikkerDigitalPost.Klient.Domene.Exceptions.SecurityException;

namespace Difi.SikkerDigitalPost.Klient.Tester.Api
{
    public class SikkerDigitalPostKlientTests
    {
        public class ConstructorMethod : SikkerDigitalPostKlientTests
        {
            [Fact]
            public void Initializes_fields()
            {
                //Arrange
                var difiOrgNr = "991825827";
                var databehandler = new Databehandler(new Organisasjonsnummer(difiOrgNr), DomainUtility.GetAvsenderCertificate());
                var klientkonfigurasjon = new Klientkonfigurasjon(Miljø.FunksjoneltTestmiljø);

                //Act
                var sikkerDigitalPostKlient = new SikkerDigitalPostKlient(databehandler, klientkonfigurasjon);

                //Assert
                Assert.Equal(klientkonfigurasjon, sikkerDigitalPostKlient.Klientkonfigurasjon);
                Assert.Equal(databehandler, sikkerDigitalPostKlient.Databehandler);
                Assert.IsType<RequestHelper>(sikkerDigitalPostKlient.RequestHelper);
            }

            [Fact]
            public void Fails_if_invalid_certificate()
            {
                //Arrange
                var databehandler = new Databehandler(new Organisasjonsnummer("988015814"),  DomainUtility.GetAvsenderEnhetstesterSertifikat());
                var klientkonfigurasjon = new Klientkonfigurasjon(Miljø.FunksjoneltTestmiljø);

                //Act
                Assert.Throws<SecurityException>(()=> new SikkerDigitalPostKlient(databehandler, klientkonfigurasjon));
            }
        }

        public class SendMethod : SikkerDigitalPostKlientTests
        {
            [Fact]
            public async Task Returns_transport_error_receipt()
            {
                //Arrange
                var sikkerDigitalPostKlient = DomainUtility.GetSikkerDigitalPostKlientQaOffentlig();
                var fakeHttpClientHandlerResponse = new FakeResponseHandler()
                {
                    HttpContent = new StringContent(XmlResource.Response.GetTransportError().OuterXml),
                    StatusCode =  HttpStatusCode.BadRequest
                };
                sikkerDigitalPostKlient.RequestHelper.HttpClient = new HttpClient(fakeHttpClientHandlerResponse);

                //Act
                var forsendelse = DomainUtility.GetForsendelseSimple();
                var transportkvittering = await sikkerDigitalPostKlient.SendAsync(forsendelse).ConfigureAwait(false);

                //Assert
                Assert.IsType<TransportFeiletKvittering>(transportkvittering);
            }

            [Fact]
            public async Task Calls_all_dokumentpakke_prosessors()
            {
                //Arrange
                var klientkonfigurasjon = new Klientkonfigurasjon(Miljø.FunksjoneltTestmiljø)
                {
                    Dokumentpakkeprosessorer = new List<IDokumentpakkeProsessor>
                    {
                        new SimpleDocumentBundleProcessor(),
                        new SimpleDocumentBundleProcessor()
                    }
                };

                var sikkerDigitalPostKlient = new SikkerDigitalPostKlient(DomainUtility.GetDatabehandler(), klientkonfigurasjon);

                DomainUtility.GetSikkerDigitalPostKlientQaOffentlig();
                var fakeHttpClientHandlerResponse = new FakeResponseHandler()
                {
                    HttpContent = new StringContent(XmlResource.Response.GetTransportOk().OuterXml),
                };
                sikkerDigitalPostKlient.RequestHelper.HttpClient = new HttpClient(fakeHttpClientHandlerResponse);

                //Act
                var forsendelse = DomainUtility.GetForsendelseSimple();

                try
                {
                    await sikkerDigitalPostKlient.SendAsync(forsendelse).ConfigureAwait(false);
                }
                catch (SecurityException)
                {
                    /*
                        We do not care about the results. Just do sending. Nasty hack as we are unable to mock validation 
                        in SikkerDigitalPostKlient, which results in invalid timestamp since response is out of date.
                    */
                }

                //Assert
                foreach (var dokumentpakkeProsessor in klientkonfigurasjon.Dokumentpakkeprosessorer.Cast<SimpleDocumentBundleProcessor>())
                {
                    Assert.True(dokumentpakkeProsessor.CouldReadBytesStream);
                }
            }

            [Fact]
            public async Task Throws_exception_on_response_not_matching_request()
            {
                //Arrange
                var sikkerDigitalPostKlient = DomainUtility.GetSikkerDigitalPostKlientQaOffentlig();
                var fakeHttpClientHandlerResponse = new FakeResponseHandler()
                {
                    HttpContent = new StringContent(XmlResource.Response.GetTransportOk().OuterXml),
                };
                sikkerDigitalPostKlient.RequestHelper.HttpClient = new HttpClient(fakeHttpClientHandlerResponse);

                //Act
                var forsendelse = DomainUtility.GetForsendelseSimple();
                await Assert.ThrowsAsync<SecurityException>(async () => await sikkerDigitalPostKlient.SendAsync(forsendelse)).ConfigureAwait(false);
            }
        }
    }
}