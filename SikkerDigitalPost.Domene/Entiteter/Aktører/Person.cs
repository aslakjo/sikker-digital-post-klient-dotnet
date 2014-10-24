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

namespace SikkerDigitalPost.Domene.Entiteter.Aktører
{
    public class Person
    {
        public string Personidentifikator { get; set; }
        public string Postkasseadresse { get; set; }

        /// <param name="personIdentifikator">Identifikator (fødselsnummer eller D-nummer)</param>
        /// <param name="postkasseadresse">Adresse hos postkasseleverandøren</param>
        public Person(string personIdentifikator, string postkasseadresse)
        {
            Personidentifikator = personIdentifikator;
            Postkasseadresse = postkasseadresse;
        }
    }
}
