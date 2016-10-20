/*
 * Copyright 2015 Esri
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
       http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
 * an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and limitations under the License.​
 */

using System;
using System.Runtime.InteropServices;
using System.Collections;

using ESRI.ArcGIS.Geodatabase;

namespace GPProcessVendorDataFunctions
{
    /// <summary>
    /// TimeZoneUtilities class has time zone string constants, a function for generating code for looking up TimeZoneID values, and a function for creating a time zone table for one time zone.
    /// </summary>
    public class TimeZoneUtilities
    {
        // Create pre-cooked constant strings with time zone names, which includes:
        //   The leading quote
        //   The five-character sort prefix (see comment below for details)
        //   The official time zone name as defined by Microsoft
        //   The trailing quote

        // The five-character sort prefix is defined as follows:
        //   First character: hours of UTC offset (A=-12, B=-11, C=-10, ..., L=-01, M=00, N=+01, ..., Y=+12, Z=+13)
        //   Second character: added minutes to UTC offset (A=+00, C=+05, E=+10, G=+15, ..., M=+30, ..., S=+45, ..., Y=+60)
        //   Third character: a dash separator
        //   Fourth character: DST present ({const}C = No DST; {north}N = DST in July; {south}S = DST in January)
        //   Fifth character: Preferred sorted order (zero-based index)

        // The list of time zones below is current as of October 2016:

        // (UTC-12:00)
        private const string DatelineST         = "\"AA-C0 Dateline Standard Time\"";           // International Date Line West
        // (UTC-11:00)
        private const string UTCMinus11         = "\"BA-C0 UTC-11\"";                           // Coordinated Universal Time-11
        // (UTC-10:00)
        private const string HawaiianST         = "\"CA-C0 Hawaiian Standard Time\"";           // Hawaii
        private const string AleutianST         = "\"CA-N0 Aleutian Standard Time\"";           // Aleutian Islands
        // (UTC-09:30)
        private const string MarquesasST        = "\"CM-C0 Marquesas Standard Time\"";          // Marquesas Islands
        // (UTC-09:00)
        private const string UTCMinus09         = "\"DA-C0 UTC-09\"";                           // Coordinated Universal Time-09
        private const string AlaskanST          = "\"DA-N0 Alaskan Standard Time\"";            // Alaska
        // (UTC-08:00)
        private const string UTCMinus08         = "\"EA-C0 UTC-08\"";                           // Coordinated Universal Time-08
        private const string PacificSTMexico    = "\"EA-N0 Pacific Standard Time (Mexico)\"";   // Baja California
        private const string PacificST          = "\"EA-N1 Pacific Standard Time\"";            // Pacific Time (US & Canada)
        // (UTC-07:00)
        private const string USMountainST       = "\"FA-C0 US Mountain Standard Time\"";        // Arizona
        private const string MountainSTMexico   = "\"FA-N0 Mountain Standard Time (Mexico)\"";  // Chihuahua, La Paz, Mazatlan
        private const string MountainST         = "\"FA-N1 Mountain Standard Time\"";           // Mountain Time (US & Canada)
        // (UTC-06:00)
        private const string CanadaCentralST    = "\"GA-C0 Canada Central Standard Time\"";     // Saskatchewan
        private const string CentralAmericaST   = "\"GA-C1 Central America Standard Time\"";    // Central America
        private const string CentralSTMexico    = "\"GA-N0 Central Standard Time (Mexico)\"";   // Guadalajara, Mexico City, Monterrey
        private const string CentralST          = "\"GA-N1 Central Standard Time\"";            // Central Time (US & Canada)
        private const string EasterIslandST     = "\"GA-S0 Easter Island Standard Time\"";      // Easter Island
        // (UTC-05:00)
        private const string EasternSTMexico    = "\"HA-C0 Eastern Standard Time (Mexico)\"";   // Chetumal
        private const string SAPacificST        = "\"HA-C1 SA Pacific Standard Time\"";         // Bogota, Lima, Quito, Rio Branco
        private const string HaitiST            = "\"HA-C2 Haiti Standard Time\"";              // Haiti
        private const string USEasternST        = "\"HA-N0 US Eastern Standard Time\"";         // Indiana (East) -- NOT USED (use Eastern instead)
        private const string EasternST          = "\"HA-N1 Eastern Standard Time\"";            // Eastern Time (US & Canada)
        private const string CubaST             = "\"HA-N2 Cuba Standard Time\"";               // Havana
        // (UTC-04:00)
        private const string TurksAndCaicosST   = "\"IA-C0 Turks And Caicos Standard Time\"";   // Turks and Caicos
        private const string VenezuelaST        = "\"IA-C1 Venezuela Standard Time\"";          // Caracas
        private const string SAWesternST        = "\"IA-C2 SA Western Standard Time\"";         // Georgetown, La Paz, Manaus, San Juan
        private const string AtlanticST         = "\"IA-N0 Atlantic Standard Time\"";           // Atlantic Time (Canada)
        private const string CentralBrazilianST = "\"IA-S0 Central Brazilian Standard Time\"";  // Cuiaba
        private const string ParaguayST         = "\"IA-S1 Paraguay Standard Time\"";           // Asuncion
        private const string PacificSAST        = "\"IA-S2 Pacific SA Standard Time\"";         // Santiago
        // (UTC-03:30)
        private const string NewfoundlandST     = "\"IM-N0 Newfoundland Standard Time\"";       // Newfoundland
        // (UTC-03:00)
        private const string ArgentinaST        = "\"JA-C0 Argentina Standard Time\"";          // City of Buenos Aires
        private const string MontevideoST       = "\"JA-C1 Montevideo Standard Time\"";         // Montevideo
        private const string SAEasternST        = "\"JA-C2 SA Eastern Standard Time\"";         // Cayenne, Fortaleza
        private const string TocantinsST        = "\"JA-C3 Tocantins Standard Time\"";          // Araguaina -- NOT USED (use SA Eastern instead)
        private const string BahiaST            = "\"JA-C4 Bahia Standard Time\"";              // Salvador -- NOT USED (use SA Eastern instead)
        private const string GreenlandST        = "\"JA-N0 Greenland Standard Time\"";          // Greenland
        private const string SaintPierreST      = "\"JA-N1 Saint Pierre Standard Time\"";       // Saint Pierre and Miquelon
        private const string ESouthAmericaST    = "\"JA-S0 E. South America Standard Time\"";   // Brasilia
        // (UTC-02:00)
        private const string UTCMinus02         = "\"KA-C0 UTC-02\"";                           // Coordinated Universal Time-02
        // (UTC-01:00)
        private const string CapeVerdeST        = "\"LA-C0 Cape Verde Standard Time\"";         // Cabo Verde Is.
        private const string AzoresST           = "\"LA-N0 Azores Standard Time\"";             // Azores
        // (UTC)
        private const string UTC                = "\"MA-00 UTC\"";                              // Coordinated Universal Time -- NOT USED
        // (UTC+00:00)
        private const string GreenwichST        = "\"MA-C0 Greenwich Standard Time\"";          // Monrovia, Reykjavik
        private const string MoroccoST          = "\"MA-N0 Morocco Standard Time\"";            // Casablanca
        private const string GMTST              = "\"MA-N1 GMT Standard Time\"";                // Dublin, Edinburgh, Lisbon, London
        // (UTC+01:00)
        private const string WCentralAfricaST   = "\"NA-C0 W. Central Africa Standard Time\"";  // West Central Africa
        private const string RomanceST          = "\"NA-N0 Romance Standard Time\"";            // Brussels, Copenhagen, Madrid, Paris
        private const string WEuropeST          = "\"NA-N1 W. Europe Standard Time\"";          // Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna
        private const string CentralEuropeST    = "\"NA-N2 Central Europe Standard Time\"";     // Belgrade, Bratislava, Budapest, Ljubljana, Prague
        private const string CentralEuropeanST  = "\"NA-N3 Central European Standard Time\"";   // Sarajevo, Skopje, Warsaw, Zagreb
        private const string NamibiaST          = "\"NA-S0 Namibia Standard Time\"";            // Windhoek
        // (UTC+02:00)
        private const string SouthAfricaST      = "\"OA-C0 South Africa Standard Time\"";       // Harare, Pretoria
        private const string LibyaST            = "\"OA-C1 Libya Standard Time\"";              // Tripoli
        private const string KaliningradST      = "\"OA-C2 Kaliningrad Standard Time\"";        // Kaliningrad
        private const string EgyptST            = "\"OA-C3 Egypt Standard Time\"";              // Cairo
        private const string FLEST              = "\"OA-N0 FLE Standard Time\"";                // Helsinki, Kyiv, Riga, Sofia, Tallinn, Vilnius
        private const string GTBST              = "\"OA-N1 GTB Standard Time\"";                // Athens, Bucharest
        private const string EEuropeST          = "\"OA-N2 E. Europe Standard Time\"";          // Chisinau
        private const string IsraelST           = "\"OA-N3 Israel Standard Time\"";             // Jerusalem
        private const string WestBankST         = "\"OA-N4 West Bank Standard Time\"";          // Gaza, Hebron
        private const string MiddleEastST       = "\"OA-N5 Middle East Standard Time\"";        // Beirut
        private const string SyriaST            = "\"OA-N6 Syria Standard Time\"";              // Damascus
        private const string JordanST           = "\"OA-N7 Jordan Standard Time\"";             // Amman
        // (UTC+03:00)
        private const string TurkeyST           = "\"PA-C0 Turkey Standard Time\"";             // Istanbul
        private const string BelarusST          = "\"PA-C1 Belarus Standard Time\"";            // Minsk
        private const string RussianST          = "\"PA-C2 Russian Standard Time\"";            // Moscow, St. Petersburg, Volgograd
        private const string EAfricaST          = "\"PA-C3 E. Africa Standard Time\"";          // Nairobi
        private const string ArabicST           = "\"PA-C4 Arabic Standard Time\"";             // Baghdad
        private const string ArabST             = "\"PA-C5 Arab Standard Time\"";               // Kuwait, Riyadh
        // (UTC+03:30)
        private const string IranST             = "\"PM-N0 Iran Standard Time\"";               // Tehran
        // (UTC+04:00)
        private const string MauritiusST        = "\"QA-C0 Mauritius Standard Time\"";          // Port Louis
        private const string ArabianST          = "\"QA-C1 Arabian Standard Time\"";            // Abu Dhabi, Muscat
        private const string GeorgianST         = "\"QA-C2 Georgian Standard Time\"";           // Tbilisi
        private const string CaucasusST         = "\"QA-C3 Caucasus Standard Time\"";           // Yerevan
        private const string AzerbaijanST       = "\"QA-C4 Azerbaijan Standard Time\"";         // Baku
        private const string AstrakhanST        = "\"QA-C5 Astrakhan Standard Time\"";          // Astrakhan, Ulyanovsk -- NOT USED (use Russia Time Zone 3 instead)
        private const string RussiaTimeZone3    = "\"QA-C6 Russia Time Zone 3\"";               // Izhevsk, Samara
        // (UTC+04:30)
        private const string AfghanistanST      = "\"QM-C0 Afghanistan Standard Time\"";        // Kabul
        // (UTC+05:00)
        private const string EkaterinburgST     = "\"RA-C0 Ekaterinburg Standard Time\"";       // Ekaterinburg
        private const string WestAsiaST         = "\"RA-C1 West Asia Standard Time\"";          // Ashgabat, Tashkent
        private const string PakistanST         = "\"RA-C2 Pakistan Standard Time\"";           // Islamabad, Karachi
        // (UTC+05:30)
        private const string IndiaST            = "\"RM-C0 India Standard Time\"";              // Chennai, Kolkata, Mumbai, New Delhi
        private const string SriLankaST         = "\"RM-C1 Sri Lanka Standard Time\"";          // Sri Jayawardenepura
        // (UTC+05:45)
        private const string NepalST            = "\"RS-C0 Nepal Standard Time\"";              // Kathmandu
        // (UTC+06:00)
        private const string CentralAsiaST      = "\"SA-C0 Central Asia Standard Time\"";       // Astana
        private const string OmskST             = "\"SA-C1 Omsk Standard Time\"";               // Omsk
        private const string BangladeshST       = "\"SA-C2 Bangladesh Standard Time\"";         // Dhaka
        // (UTC+06:30)
        private const string MyanmarST          = "\"SM-C0 Myanmar Standard Time\"";            // Yangon (Rangoon)
        // (UTC+07:00)
        private const string NCentralAsiaST     = "\"TA-C0 N. Central Asia Standard Time\"";    // Novosibirsk -- NOT USED (use North Asia instead)
        private const string AltaiST            = "\"TA-C1 Altai Standard Time\"";              // Barnaul, Gorno-Altaysk -- NOT USED (use North Asia instead)
        private const string TomskST            = "\"TA-C2 Tomsk Standard Time\"";              // Tomsk -- NOT USED (use North Asia instead)
        private const string NorthAsiaST        = "\"TA-C3 North Asia Standard Time\"";         // Krasnoyarsk
        private const string SEAsiaST           = "\"TA-C4 SE Asia Standard Time\"";            // Bangkok, Hanoi, Jakarta
        private const string WMongoliaST        = "\"TA-N0 W. Mongolia Standard Time\"";        // Hovd
        // (UTC+08:00)
        private const string NorthAsiaEastST    = "\"UA-C0 North Asia East Standard Time\"";    // Irkutsk
        private const string ChinaST            = "\"UA-C1 China Standard Time\"";              // Beijing, Chongqing, Hong Kong, Urumqi
        private const string TaipeiST           = "\"UA-C2 Taipei Standard Time\"";             // Taipei
        private const string SingaporeST        = "\"UA-C3 Singapore Standard Time\"";          // Kuala Lumpur, Singapore
        private const string WAustraliaST       = "\"UA-C4 W. Australia Standard Time\"";       // Perth
        private const string UlaanbaatarST      = "\"UA-N0 Ulaanbaatar Standard Time\"";        // Ulaanbaatar
        // (UTC+08:30)
        private const string NorthKoreaST       = "\"UM-C0 North Korea Standard Time\"";        // Pyongyang
        // (UTC+08:45)
        private const string AusCentralWST      = "\"US-C0 Aus Central W. Standard Time\"";     // Eucla
        // (UTC+09:00)
        private const string TransbaikalST      = "\"VA-C0 Transbaikal Standard Time\"";        // Chita -- NOT USED (use Yakutsk instead)
        private const string YakutskST          = "\"VA-C1 Yakutsk Standard Time\"";            // Yakutsk
        private const string KoreaST            = "\"VA-C2 Korea Standard Time\"";              // Seoul
        private const string TokyoST            = "\"VA-C3 Tokyo Standard Time\"";              // Osaka, Sapporo, Tokyo
        // (UTC+09:30)
        private const string AusCentralST       = "\"VM-C0 AUS Central Standard Time\"";        // Darwin
        private const string CenAustraliaST     = "\"VM-S0 Cen. Australia Standard Time\"";     // Adelaide
        // (UTC+10:00)
        private const string VladivostokST      = "\"WA-C0 Vladivostok Standard Time\"";        // Vladivostok
        private const string WestPacificST      = "\"WA-C1 West Pacific Standard Time\"";       // Guam, Port Moresby
        private const string EAustraliaST       = "\"WA-C2 E. Australia Standard Time\"";       // Brisbane
        private const string AusEasternST       = "\"WA-S0 AUS Eastern Standard Time\"";        // Canberra, Melbourne, Sydney
        private const string TasmaniaST         = "\"WA-S1 Tasmania Standard Time\"";           // Hobart -- NOT USED (use AUS Eastern instead)
        // (UTC+10:30)
        private const string LordHoweST         = "\"WM-S0 Lord Howe Standard Time\"";          // Lord Howe Island
        // (UTC+11:00)
        private const string SakhalinST         = "\"XA-C0 Sakhalin Standard Time\"";           // Sakhalin -- NOT USED (use Russia Time Zone 10 instead)
        private const string RussiaTimeZone10   = "\"XA-C1 Russia Time Zone 10\"";              // Chokurdakh
        private const string MagadanST          = "\"XA-C2 Magadan Standard Time\"";            // Magadan -- NOT USED (use Russia Time Zone 10 instead)
        private const string BougainvilleST     = "\"XA-C3 Bougainville Standard Time\"";       // Bougainville Island
        private const string CentralPacificST   = "\"XA-C4 Central Pacific Standard Time\"";    // Solomon Is., New Caledonia
        private const string NorfolkST          = "\"XA-C5 Norfolk Standard Time\"";            // Norfolk Island
        // (UTC+12:00)
        private const string RussiaTimeZone11   = "\"YA-C0 Russia Time Zone 11\"";              // Anadyr, Petropavlovsk-Kamchatsky
        private const string UTCPlus12          = "\"YA-C1 UTC+12\"";                           // Coordinated Universal Time+12
        private const string FijiST             = "\"YA-S0 Fiji Standard Time\"";               // Fiji
        private const string NewZealandST       = "\"YA-S1 New Zealand Standard Time\"";        // Auckland, Wellington
        // (UTC+12:45)
        private const string ChathamIslandsST   = "\"YS-S0 Chatham Islands Standard Time\"";    // Chatham Islands
        // (UTC+13:00)
        private const string TongaST            = "\"ZA-C0 Tonga Standard Time\"";              // Nuku'alofa
        private const string SamoaST            = "\"ZA-S0 Samoa Standard Time\"";              // Samoa
        // (UTC+14:00)
        private const string LineIslandsST      = "\"ZY-C0 Line Islands Standard Time\"";       // Kiritimati Island

        public TimeZoneUtilities()
        {
        }

        public static string MakeSortableMSTIMEZONECode(string countryCodeFieldName)
        {
            return "utcOffset = [UTCOffset]" +
                   "\nIf IsNull(utcOffset) Then" +
                   "\n  z = Null" +
                   "\nElseIf utcOffset < -690 Then" +    // UTC-12:00 == -720
                   "\n  z = " + DatelineST +
                   "\nElseIf utcOffset < -630 Then" +    // UTC-11:00 == -660
                   "\n  z = " + UTCMinus11 +
                   "\nElseIf utcOffset < -585 Then" +    // UTC-10:00 == -600
                   "\n  z = " + HawaiianST + ": If [" + countryCodeFieldName + "] = \"USA\" And [DST] > 0 Then z = " + AleutianST +
                   "\nElseIf utcOffset < -555 Then" +    // UTC-09:30 == -570
                   "\n  z = " + MarquesasST +
                   "\nElseIf utcOffset < -510 Then" +    // UTC-09:00 == -540
                   "\n  z = " + UTCMinus09 + ": If [" + countryCodeFieldName + "] = \"USA\" And [DST] > 0 Then z = " + AlaskanST +
                   "\nElseIf utcOffset < -450 Then" +    // UTC-08:00 == -480
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"USA\", \"CAN\"" +
                   "\n      If [DST] > 0 Then z = " + PacificST + " Else z = " + UTCMinus08 +
                   "\n    Case \"MEX\"" +
                   "\n      If [DST] > 0 Then z = " + PacificSTMexico + " Else z = " + UTCMinus08 +
                   "\n    Case Else: z = " + UTCMinus08 +
                   "\n  End Select" +
                   "\nElseIf utcOffset < -390 Then" +    // UTC-07:00 == -420
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"USA\", \"CAN\"" +
                   "\n      If [DST] > 0 Then z = " + MountainST + " Else z = " + USMountainST +
                   "\n    Case \"MEX\"" +
                   "\n      If [DST] > 0 Then z = " + MountainSTMexico + " Else z = " + USMountainST +
                   "\n    Case Else: z = " + USMountainST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < -330 Then" +    // UTC-06:00 == -360
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"CAN\"" +
                   "\n      If [DST] > 0 Then z = " + CentralST + " Else z = " + CanadaCentralST +
                   "\n    Case \"USA\": z = " + CentralST +
                   "\n    Case \"MEX\": z = " + CentralSTMexico +
                   "\n    Case \"CHL\": z = " + EasterIslandST +
                   "\n    Case Else: z = " + CentralAmericaST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < -270 Then" +    // UTC-05:00 == -300
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"CAN\"" +
                   "\n      If [DST] > 0 Then z = " + EasternST + " Else z = " + SAPacificST +
                   "\n    Case \"USA\", \"BHS\": z = " + EasternST +
                   "\n    Case \"MEX\": z = " + EasternSTMexico +
                   "\n    Case \"CUB\": z = " + CubaST +
                   "\n    Case \"HTI\": z = " + HaitiST +
                   "\n    Case Else: z = " + SAPacificST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < -225 Then" +    // UTC-04:00 == -240
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"CAN\"" +
                   "\n      If [DST] > 0 Then z = " + AtlanticST + " Else z = " + SAWesternST +
                   "\n    Case \"GRL\", \"BMU\": z = " + AtlanticST +
                   "\n    Case \"TCA\": z = " + TurksAndCaicosST +
                   "\n    Case \"BRA\"" +
                   "\n      If [DST] > 0 Then z = " + CentralBrazilianST + " Else z = " + SAWesternST +
                   "\n    Case \"VEN\": z = " + VenezuelaST +
                   "\n    Case \"CHL\": z = " + PacificSAST +
                   "\n    Case \"PRY\": z = " + ParaguayST +
                   "\n    Case Else: z = " + SAWesternST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < -195 Then" +    // UTC-03:30 == -210
                   "\n  z = " + NewfoundlandST +
                   "\nElseIf utcOffset < -150 Then" +    // UTC-03:00 == -180
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"GRL\": z = " + GreenlandST +
                   "\n    Case \"SPM\": z = " + SaintPierreST +
                   "\n    Case \"BRA\"" +
                   "\n      If [DST] > 0 Then z = " + ESouthAmericaST + " Else z = " + SAEasternST +
                   "\n    Case \"ARG\": z = " + ArgentinaST +
                   "\n    Case \"URY\": z = " + MontevideoST +
                   "\n    Case Else: z = " + SAEasternST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < -90 Then" +    // UTC-02:00 == -120
                   "\n  z = " + UTCMinus02 +
                   "\nElseIf utcOffset < -30 Then" +    // UTC-01:00 == -60
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"GRL\", \"PRT\": z = " + AzoresST +
                   "\n    Case Else: z = " + CapeVerdeST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 30 Then" +    // UTC+00:00 == 0
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"GBR\", \"IRL\", \"IMN\", \"JEY\", \"GGY\", \"FRO\", \"PRT\", \"ESP\": z = " + GMTST +
                   "\n    Case \"MAR\", \"ESH\": z = " + MoroccoST +
                   "\n    Case Else: z = " + GreenwichST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 90 Then" +    // UTC+01:00 == 60
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"NOR\", \"SWE\", \"SJM\", \"NLD\", \"DEU\", \"CHE\", \"AUT\", \"LIE\", \"ITA\", \"SMR\", \"VAT\", \"MLT\": z = " + WEuropeST +
                   "\n    Case \"ESP\", \"GIB\", \"FRA\", \"AND\", \"MCO\", \"BEL\", \"LUX\", \"DNK\": z = " + RomanceST +
                   "\n    Case \"CZE\", \"SVK\", \"HUN\", \"SVN\", \"SRB\", \"KOS\", \"MNE\": z = " + CentralEuropeST +
                   "\n    Case \"POL\", \"HRV\", \"BIH\", \"ALB\", \"MKD\": z = " + CentralEuropeanST +
                   "\n    Case \"NAM\": z = " + NamibiaST +
                   "\n    Case Else: z = " + WCentralAfricaST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 150 Then" +    // UTC+02:00 == 120
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"FIN\", \"EST\", \"LVA\", \"LTU\", \"UKR\", \"BGR\": z = " + FLEST +
                   "\n    Case \"GRC\", \"ROU\": z = " + GTBST +
                   "\n    Case \"CYP\", \"BSB\", \"CUN\", \"NCY\", \"MDA\": z = " + EEuropeST +
                   "\n    Case \"RUS\": z = " + KaliningradST +
                   "\n    Case \"LBY\": z = " + LibyaST +
                   "\n    Case \"EGY\": z = " + EgyptST +
                   "\n    Case \"SYR\": z = " + SyriaST +
                   "\n    Case \"LBN\": z = " + MiddleEastST +
                   "\n    Case \"JOR\": z = " + JordanST +
                   "\n    Case \"ISR\": z = " + IsraelST +
                   "\n    Case \"PSE\", \"WEB\", \"GAS\": z = " + WestBankST +
                   "\n    Case Else: z = " + SouthAfricaST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 195 Then" +    // UTC+03:00 == 180
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"BLR\": z = " + BelarusST +
                   "\n    Case \"RUS\": z = " + RussianST +
                   "\n    Case \"TUR\": z = " + TurkeyST +
                   "\n    Case \"IRQ\": z = " + ArabicST +
                   "\n    Case \"SAU\", \"KWT\", \"BHR\", \"QAT\", \"YEM\": z = " + ArabST +
                   "\n    Case Else: z = " + EAfricaST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 225 Then" +    // UTC+03:30 == 210
                   "\n  z = " + IranST +
                   "\nElseIf utcOffset < 255 Then" +    // UTC+04:00 == 240
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"RUS\": z = " + RussiaTimeZone3 +
                   "\n    Case \"GEO\": z = " + GeorgianST +
                   "\n    Case \"ARM\": z = " + CaucasusST +
                   "\n    Case \"AZE\": z = " + AzerbaijanST +
                   "\n    Case \"MUS\": z = " + MauritiusST +
                   "\n    Case Else: z = " + ArabianST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 295 Then" +    // UTC+04:30 == 270
                   "\n  z = " + AfghanistanST +
                   "\nElseIf utcOffset < 315 Then" +    // UTC+05:00 == 300
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"RUS\": z = " + EkaterinburgST +
                   "\n    Case \"PAK\": z = " + PakistanST +
                   "\n    Case Else: z = " + WestAsiaST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 338 Then" +    // UTC+05:30 == 330
                   "\n  z = " + IndiaST + ": If [" + countryCodeFieldName + "] = \"LKA\" Then z = " + SriLankaST +
                   "\nElseIf utcOffset < 353 Then" +    // UTC+05:45 == 345
                   "\n  z = " + NepalST +
                   "\nElseIf utcOffset < 375 Then" +    // UTC+06:00 == 360
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"RUS\": z = " + OmskST +
                   "\n    Case \"BGD\": z = " + BangladeshST +
                   "\n    Case Else: z = " + CentralAsiaST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 405 Then" +    // UTC+06:30 == 390
                   "\n  z = " + MyanmarST +
                   "\nElseIf utcOffset < 450 Then" +    // UTC+07:00 == 420
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"RUS\": z = " + NorthAsiaST +
                   "\n    Case \"MNG\": z = " + WMongoliaST +
                   "\n    Case Else: z = " + SEAsiaST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 495 Then" +    // UTC+08:00 == 480
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"RUS\": z = " + NorthAsiaEastST +
                   "\n    Case \"MNG\": z = " + UlaanbaatarST +
                   "\n    Case \"CHN\", \"HKG\", \"MAC\": z = " + ChinaST +
                   "\n    Case \"TWN\": z = " + TaipeiST +
                   "\n    Case \"AUS\": z = " + WAustraliaST +
                   "\n    Case Else: z = " + SingaporeST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 518 Then" +    // UTC+08:30 == 510
                   "\n  z = " + NorthKoreaST +
                   "\nElseIf utcOffset < 533 Then" +    // UTC+08:45 == 525
                   "\n  z = " + AusCentralWST +
                   "\nElseIf utcOffset < 555 Then" +    // UTC+09:00 == 540
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"RUS\": z = " + YakutskST +
                   "\n    Case \"KOR\": z = " + KoreaST +
                   "\n    Case Else: z = " + TokyoST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 585 Then" +    // UTC+09:30 == 570
                   "\n  z = " + AusCentralST + ": If [DST] > 0 Then z = " + CenAustraliaST +
                   "\nElseIf utcOffset < 615 Then" +    // UTC+10:00 == 600
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"RUS\": z = " + VladivostokST +
                   "\n    Case \"AUS\"" +
                   "\n      If [DST] > 0 Then z = " + AusEasternST + " Else z = " + EAustraliaST +
                   "\n    Case Else: z = " + WestPacificST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 645 Then" +    // UTC+10:30 == 630
                   "\n  z = " + LordHoweST +
                   "\nElseIf utcOffset < 690 Then" +    // UTC+11:00 == 660
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"RUS\": z = " + RussiaTimeZone10 +
                   "\n    Case \"PNG\": z = " + BougainvilleST +
                   "\n    Case \"AUS\", \"NFK\": z = " + NorfolkST +
                   "\n    Case Else: z = " + CentralPacificST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 743 Then" +    // UTC+12:00 == 720
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"RUS\": z = " + RussiaTimeZone11 +
                   "\n    Case \"FJI\": z = " + FijiST +
                   "\n    Case \"NZL\": z = " + NewZealandST +
                   "\n    Case Else: z = " + UTCPlus12 +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 773 Then" +    // UTC+12:45 == 765
                   "\n  z = " + ChathamIslandsST +
                   "\nElseIf utcOffset < 810 Then" +    // UTC+13:00 == 780
                   "\n  z = " + TongaST + ": If [" + countryCodeFieldName + "] = \"WSM\" Then z = " + SamoaST +
                   "\nElse" +    // UTC+14:00 == 840
                   "\n  z = " + LineIslandsST +
                   "\nEnd If";
        }

        public static string MakeTimeZoneIDCode(string outputFileGdbPath, string timeZoneTableName, string fieldName)
        {
            // Open the Time Zone Table and find the MSTIMEZONE field

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var gdbWSF = Activator.CreateInstance(factoryType) as IWorkspaceFactory;
            var gdbFWS = gdbWSF.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;
            ITable timeZoneTable = gdbFWS.OpenTable(timeZoneTableName);
            int msTimeZoneField = timeZoneTable.FindField("MSTIMEZONE");

            // Loop through the table to generate the code

            string s = "tzID = Null\nsortableName = [" + fieldName + "]\nSelect Case Right(sortableName, Len(sortableName) - 6)";
            ICursor cur = timeZoneTable.Search(null, true);
            IRow inputTableRow = null;
            while ((inputTableRow = cur.NextRow()) != null)
            {
                s = s + "\n  Case \"" + inputTableRow.get_Value(msTimeZoneField) + "\": tzID = " + Convert.ToString(inputTableRow.OID, System.Globalization.CultureInfo.InvariantCulture);
            }
            s = s + "\nEnd Select";

            return s;
        }

        public static ITable CreateTimeZoneTable(string outputFileGdbPath, string timeZone)
        {
            // start with the initial set of required fields for a table

            IObjectClassDescription ocDescription = new ObjectClassDescriptionClass();
            IFieldsEdit outFields = ocDescription.RequiredFields as IFieldsEdit;

            // add the MSTIMEZONE field to the table

            IFieldEdit field = new FieldClass();
            field.Name_2 = "MSTIMEZONE";
            field.Type_2 = esriFieldType.esriFieldTypeString;
            field.Length_2 = 50;
            outFields.AddField(field);

            // open the file geodatabase

            Type gdbFactoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var gdbWSF = Activator.CreateInstance(gdbFactoryType) as IWorkspaceFactory;
            var gdbFWS = gdbWSF.OpenFromFile(outputFileGdbPath, 0) as IFeatureWorkspace;

            // create the table

            ITable t = gdbFWS.CreateTable("TimeZones", outFields, ocDescription.InstanceCLSID, ocDescription.ClassExtensionCLSID, "");

            // create a record in the table with the specified time zone

            ICursor cur = t.Insert(true);
            IRowBuffer buff = t.CreateRowBuffer();
            buff.set_Value(t.FindField("MSTIMEZONE"), timeZone);
            cur.InsertRow(buff);

            // Flush any outstanding writes to the table
            cur.Flush();

            return t;
        }
    }
}
