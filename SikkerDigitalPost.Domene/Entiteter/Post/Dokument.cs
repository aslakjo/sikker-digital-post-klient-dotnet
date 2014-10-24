﻿/** 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *         http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.IO;
using SikkerDigitalPost.Domene.Entiteter.Interface;

namespace SikkerDigitalPost.Domene.Entiteter.Post
{
    public class Dokument : IAsiceVedlegg
    {
        public string Tittel { get; private set; }
        public string Filnavn { get; private set; }
        public byte[] Bytes { get; private set; }
        public string Innholdstype { get; private set; }
        public string Id { get; set; }
        public string Språkkode { get; private set; }

        /// <param name="tittel">Tittel som vises til brukeren gitt riktig sikkerhetsnivå.</param>
        /// <param name="dokumentsti">Stien som viser til hvor dokumentet ligger på disk.</param>
        /// <param name="innholdstype">Innholdstype for dokumentet. For informasjon om tillatte formater, se http://begrep.difi.no/SikkerDigitalPost/1.0.3/forretningslag/Dokumentformat/. </param>
        /// <param name="språkkode">Språkkode for dokumentet. Om ikke satt, brukes <see cref="Forsendelse"/> sitt språk.</param>
        /// <param name="filnavn">Filnavnet til dokumentet.</param>
        public Dokument(string tittel, string dokumentsti, string innholdstype, string språkkode = null, string filnavn = null)
            : this(tittel, File.ReadAllBytes(dokumentsti), innholdstype, språkkode, filnavn ?? Path.GetFileName(dokumentsti))
        {
        }

        /// <param name="tittel">Tittel som vises til brukeren gitt riktig sikkerhetsnivå.</param>
        /// <param name="dokumentstrøm">Dokumentet representert som en strøm.</param>
        /// <param name="innholdstype">Innholdstype for dokumentet. For informasjon om tillatte formater, se http://begrep.difi.no/SikkerDigitalPost/1.0.3/forretningslag/Dokumentformat/. </param>
        /// <param name="språkkode">Språkkode for dokumentet. Om ikke satt, brukes <see cref="Forsendelse"/> sitt språk.</param>
        /// <param name="filnavn">Filnavnet til dokumentet.</param>
        public Dokument(string tittel, Stream dokumentstrøm, string innholdstype, string språkkode = null, string filnavn = null)
            : this(tittel, File.ReadAllBytes(new StreamReader(dokumentstrøm).ReadToEnd()), innholdstype, språkkode, filnavn)
        {
        }

        /// <param name="tittel">Tittel som vises til brukeren gitt riktig sikkerhetsnivå.</param>
        /// <param name="dokumentbytes">Dokumentet representert som byte[].</param>
        /// <param name="innholdstype">Innholdstype for dokumentet. For informasjon om tillatte formater, se http://begrep.difi.no/SikkerDigitalPost/1.0.3/forretningslag/Dokumentformat/. </param>
        /// <param name="språkkode">Språkkode for dokumentet. Om ikke satt, brukes <see cref="Forsendelse"/> sitt språk.</param>
        /// <param name="filnavn">Filnavnet til dokumentet.</param>
        public Dokument(string tittel, byte[] dokumentbytes, string innholdstype, string språkkode = null, string filnavn = null)
        {
            Tittel = tittel;
            Bytes = dokumentbytes;
            Innholdstype = innholdstype;
            Filnavn = filnavn ?? Path.GetRandomFileName();
            Språkkode = språkkode;
        }
    }
}
