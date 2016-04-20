﻿using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Difi.SikkerDigitalPost.Klient.Domene.Entiteter.Kvitteringer.Transport;
using Difi.SikkerDigitalPost.Klient.Internal;
using Difi.SikkerDigitalPost.Klient.Internal.AsicE;
using Difi.SikkerDigitalPost.Klient.Tester.Fakes;
using Difi.SikkerDigitalPost.Klient.Tester.Utilities;
using Difi.SikkerDigitalPost.Klient.Utilities;
using Difi.SikkerDigitalPost.Klient.XmlValidering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Difi.SikkerDigitalPost.Klient.Tester.Internal
{
    [TestClass]
    public class RequestHelperTests
    {
        [TestClass]
        public class ConstructorMethod : RequestHelperTests
        {
            [TestMethod]
            public void InitializesFields()
            {
                //Arrange
                var clientConfiguration = new Klientkonfigurasjon(Miljø.FunksjoneltTestmiljø);

                //Act
                var requestHelper = new RequestHelper(clientConfiguration);

                //Assert
                Assert.AreEqual(clientConfiguration, requestHelper.ClientConfiguration);
            } 
        }

        [TestClass]
        public class SendMethod : RequestHelperTests
        {
            [TestMethod]
            public async Task ReturnsReceiptSuccessfully()
            {
                //Arrange
                var forretningsmeldingEnvelope = DomeneUtility.GetForretningsmeldingEnvelope();

                var documentBundle = AsiceGenerator.Create(DomeneUtility.GetDigitalForsendelseEnkel(), new GuidUtility(), DomeneUtility.GetAvsenderSertifikat());

                var requestHelper = new RequestHelper(new Klientkonfigurasjon(Miljø.FunksjoneltTestmiljø));
                var fakeHttpClientHandlerResponse = new FakeHttpClientHandlerResponse(Resources.Xml.XmlResource.Response.GetTransportOk().OuterXml, HttpStatusCode.OK);
                requestHelper.HttpClient = new HttpClient(fakeHttpClientHandlerResponse);

                
                //Act 
                var kvittering = await requestHelper.SendMessage(forretningsmeldingEnvelope, documentBundle);

                //Assert
                Assert.IsInstanceOfType(kvittering, typeof(TransportOkKvittering));
            }
        }

        [TestClass]
        public class GetReceiptMethod : RequestHelperTests
        {
            [TestMethod]
            public void GetsReceiptSuccessfully()
            {
                //Arrange
                

                //Act

                //Assert
                Assert.Fail();
            } 
        }

        [TestClass]
        public class ConfirmReceiptMethod : RequestHelperTests
        {
            [TestMethod]
            public void ConfirmsReceiptSuccessfully()
            {
                //Arrange


                //Act

                //Assert
                Assert.Fail();
            }
        }

    }
}
