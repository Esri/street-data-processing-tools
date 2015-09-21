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

        // The list of time zones below is current as of August 2015:

        // (UTC-12:00)
        private const string DatelineST         = "\"AA-C0 Dateline Standard Time\"";           // International Date Line West
        // (UTC-11:00)
        private const string UTCMinus11         = "\"BA-C0 UTC-11\"";                           // Coordinated Universal Time-11
        // (UTC-10:00)
        private const string HawaiianST         = "\"CA-C0 Hawaiian Standard Time\"";           // Hawaii
        // (UTC-09:00)
        private const string AlaskanST          = "\"DA-N0 Alaskan Standard Time\"";            // Alaska
        // (UTC-08:00)
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
        // (UTC-05:00)
        private const string EasternSTMexico    = "\"HA-C0 Eastern Standard Time (Mexico)\"";   // Chetumal
        private const string SAPacificST        = "\"HA-C1 SA Pacific Standard Time\"";         // Bogota, Lima, Quito, Rio Branco
        private const string USEasternST        = "\"HA-N0 US Eastern Standard Time\"";         // Indiana (East) -- NOT USED (use Eastern instead)
        private const string EasternST          = "\"HA-N1 Eastern Standard Time\"";            // Eastern Time (US & Canada)
        // (UTC-04:30)
        private const string VenezuelaST        = "\"HM-C0 Venezuela Standard Time\"";          // Caracas
        // (UTC-04:00)
        private const string SAWesternST        = "\"IA-C0 SA Western Standard Time\"";         // Georgetown, La Paz, Manaus, San Juan
        private const string AtlanticST         = "\"IA-N0 Atlantic Standard Time\"";           // Atlantic Time (Canada)
        private const string CentralBrazilianST = "\"IA-S0 Central Brazilian Standard Time\"";  // Cuiaba
        private const string ParaguayST         = "\"IA-S1 Paraguay Standard Time\"";           // Asuncion
        // (UTC-03:30)
        private const string NewfoundlandST     = "\"IM-N0 Newfoundland Standard Time\"";       // Newfoundland
        // (UTC-03:00)
        private const string PacificSAST        = "\"JA-C0 Pacific SA Standard Time\"";         // Santiago
        private const string ArgentinaST        = "\"JA-C1 Argentina Standard Time\"";          // Buenos Aires
        private const string MontevideoST       = "\"JA-C2 Montevideo Standard Time\"";         // Montevideo
        private const string SAEasternST        = "\"JA-C3 SA Eastern Standard Time\"";         // Cayenne, Fortaleza
        private const string BahiaST            = "\"JA-C4 Bahia Standard Time\"";              // Salvador -- NOT USED (use SA Eastern instead)
        private const string GreenlandST        = "\"JA-N0 Greenland Standard Time\"";          // Greenland
        private const string ESouthAmericaST    = "\"JA-S0 E. South America Standard Time\"";   // Brasilia
        // (UTC-02:00)
        private const string UTCMinus02         = "\"KA-C0 UTC-02\"";                           // Coordinated Universal Time-02
        private const string MidAtlanticST      = "\"KA-N0 Mid-Atlantic Standard Time\"";       // Mid-Atlantic
        // (UTC-01:00)
        private const string CapeVerdeST        = "\"LA-C0 Cape Verde Standard Time\"";         // Cabo Verde Is.
        private const string AzoresST           = "\"LA-N0 Azores Standard Time\"";             // Azores
        // (UTC)
        private const string UTC                = "\"MA-C0 UTC\"";                              // Coordinated Universal Time
        private const string GreenwichST        = "\"MA-C1 Greenwich Standard Time\"";          // Monrovia, Reykjavik
        private const string MoroccoST          = "\"MA-N0 Morocco Standard Time\"";            // Casablanca
        private const string GMTST              = "\"MA-N1 GMT Standard Time\"";                // Greenwich, Dublin, Edinburgh, Lisbon, London
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
        private const string KaliningradST      = "\"OA-C2 Kaliningrad Standard Time\"";        // Kaliningrad (RTZ 1)
        private const string FLEST              = "\"OA-N0 FLE Standard Time\"";                // Helsinki, Kyiv, Riga, Sofia, Tallinn, Vilnius
        private const string GTBST              = "\"OA-N1 GTB Standard Time\"";                // Athens, Bucharest
        private const string TurkeyST           = "\"OA-N2 Turkey Standard Time\"";             // Istanbul
        private const string EEuropeST          = "\"OA-N3 E. Europe Standard Time\"";          // E. Europe
        private const string EgyptST            = "\"OA-N4 Egypt Standard Time\"";              // Cairo
        private const string IsraelST           = "\"OA-N5 Israel Standard Time\"";             // Jerusalem
        private const string MiddleEastST       = "\"OA-N6 Middle East Standard Time\"";        // Beirut
        private const string SyriaST            = "\"OA-N7 Syria Standard Time\"";              // Damascus
        private const string JordanST           = "\"OA-N8 Jordan Standard Time\"";             // Amman
        // (UTC+03:00)
        private const string BelarusST          = "\"PA-C0 Belarus Standard Time\"";            // Minsk
        private const string RussianST          = "\"PA-C1 Russian Standard Time\"";            // Moscow, St. Petersburg, Volgograd (RTZ 2)
        private const string EAfricaST          = "\"PA-C2 E. Africa Standard Time\"";          // Nairobi
        private const string ArabicST           = "\"PA-C3 Arabic Standard Time\"";             // Baghdad
        private const string ArabST             = "\"PA-C4 Arab Standard Time\"";               // Kuwait, Riyadh
        // (UTC+03:30)
        private const string IranST             = "\"PM-N0 Iran Standard Time\"";               // Tehran
        // (UTC+04:00)
        private const string MauritiusST        = "\"QA-C0 Mauritius Standard Time\"";          // Port Louis
        private const string ArabianST          = "\"QA-C1 Arabian Standard Time\"";            // Abu Dhabi, Muscat
        private const string GeorgianST         = "\"QA-C2 Georgian Standard Time\"";           // Tbilisi
        private const string CaucasusST         = "\"QA-C3 Caucasus Standard Time\"";           // Yerevan
        private const string RussiaTimeZone3    = "\"QA-C4 Russia Time Zone 3\"";               // Izhevsk, Samara (RTZ 3)
        private const string AzerbaijanST       = "\"QA-N0 Azerbaijan Standard Time\"";         // Baku
        // (UTC+04:30)
        private const string AfghanistanST      = "\"QM-C0 Afghanistan Standard Time\"";        // Kabul
        // (UTC+05:00)
        private const string EkaterinburgST     = "\"RA-C0 Ekaterinburg Standard Time\"";       // Ekaterinburg (RTZ 4)
        private const string WestAsiaST         = "\"RA-C1 West Asia Standard Time\"";          // Ashgabat, Tashkent
        private const string PakistanST         = "\"RA-C2 Pakistan Standard Time\"";           // Islamabad, Karachi
        // (UTC+05:30)
        private const string IndiaST            = "\"RM-C0 India Standard Time\"";              // Chennai, Kolkata, Mumbai, New Delhi
        private const string SriLankaST         = "\"RM-C1 Sri Lanka Standard Time\"";          // Sri Jayawardenepura
        // (UTC+05:45)
        private const string NepalST            = "\"RS-C0 Nepal Standard Time\"";              // Kathmandu
        // (UTC+06:00)
        private const string CentralAsiaST      = "\"SA-C0 Central Asia Standard Time\"";       // Astana
        private const string NCentralAsiaST     = "\"SA-C1 N. Central Asia Standard Time\"";    // Novosibirsk (RTZ 5)
        private const string BangladeshST       = "\"SA-C2 Bangladesh Standard Time\"";         // Dhaka
        // (UTC+06:30)
        private const string MyanmarST          = "\"SM-C0 Myanmar Standard Time\"";            // Yangon (Rangoon)
        // (UTC+07:00)
        private const string NorthAsiaST        = "\"TA-C0 North Asia Standard Time\"";         // Krasnoyarsk (RTZ 6)
        private const string SEAsiaST           = "\"TA-C1 SE Asia Standard Time\"";            // Bangkok, Hanoi, Jakarta
        // (UTC+08:00)
        private const string NorthAsiaEastST    = "\"UA-C0 North Asia East Standard Time\"";    // Irkutsk (RTZ 7)
        private const string ChinaST            = "\"UA-C1 China Standard Time\"";              // Beijing, Chongqing, Hong Kong, Urumqi
        private const string TaipeiST           = "\"UA-C2 Taipei Standard Time\"";             // Taipei
        private const string SingaporeST        = "\"UA-C3 Singapore Standard Time\"";          // Kuala Lumpur, Singapore
        private const string WAustraliaST       = "\"UA-C4 W. Australia Standard Time\"";       // Perth
        private const string UlaanbaatarST      = "\"UA-N0 Ulaanbaatar Standard Time\"";        // Ulaanbaatar
        // (UTC+09:00)
        private const string YakutskST          = "\"VA-C0 Yakutsk Standard Time\"";            // Yakutsk (RTZ 8)
        private const string KoreaST            = "\"VA-C1 Korea Standard Time\"";              // Seoul
        private const string TokyoST            = "\"VA-C2 Tokyo Standard Time\"";              // Osaka, Sapporo, Tokyo
        // (UTC+09:30)
        private const string AusCentralST       = "\"VM-C0 AUS Central Standard Time\"";        // Darwin
        private const string CenAustraliaST     = "\"VM-S0 Cen. Australia Standard Time\"";     // Adelaide
        // (UTC+10:00)
        private const string VladivostokST      = "\"WA-C0 Vladivostok Standard Time\"";        // Vladivostok, Magadan (RTZ 9)
        private const string MagadanST          = "\"WA-C1 Magadan Standard Time\"";            // Magadan -- NOT USED (use Vladivostok instead)
        private const string WestPacificST      = "\"WA-C2 West Pacific Standard Time\"";       // Guam, Port Moresby
        private const string EAustraliaST       = "\"WA-C3 E. Australia Standard Time\"";       // Brisbane
        private const string AusEasternST       = "\"WA-S0 AUS Eastern Standard Time\"";        // Canberra, Melbourne, Sydney
        private const string TasmaniaST         = "\"WA-S1 Tasmania Standard Time\"";           // Hobart -- NOT USED (use AUS Eastern instead)
        // (UTC+11:00)
        private const string RussiaTimeZone10   = "\"XA-C0 Russia Time Zone 10\"";              // Chokurdakh (RTZ 10)
        private const string CentralPacificST   = "\"XA-C1 Central Pacific Standard Time\"";    // Solomon Is., New Caledonia
        // (UTC+12:00)
        private const string RussiaTimeZone11   = "\"YA-C0 Russia Time Zone 11\"";              // Anadyr, Petropavlovsk-Kamchatsky (RTZ 11)
        private const string UTCPlus12          = "\"YA-C1 UTC+12\"";                           // Coordinated Universal Time+12
        private const string FijiST             = "\"YA-S0 Fiji Standard Time\"";               // Fiji
        private const string NewZealandST       = "\"YA-S1 New Zealand Standard Time\"";        // Auckland, Wellington
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
                   "\nElseIf utcOffset < -570 Then" +    // UTC-10:00 == -600
                   "\n  z = " + HawaiianST +
                   "\nElseIf utcOffset < -510 Then" +    // UTC-09:00 == -540
                   "\n  z = " + AlaskanST +
                   "\nElseIf utcOffset < -450 Then" +    // UTC-08:00 == -480
                   "\n  z = " + PacificST + ": If [" + countryCodeFieldName + "] = \"MEX\" Then z = " + PacificSTMexico +
                   "\nElseIf utcOffset < -390 Then" +    // UTC-07:00 == -420
                   "\n  If [DST] > 0 Then" +
                   "\n    z = " + MountainST + ": If [" + countryCodeFieldName + "] = \"MEX\" Then z = " + MountainSTMexico +
                   "\n  Else" +
                   "\n    z = " + USMountainST +
                   "\n  End If" +
                   "\nElseIf utcOffset < -330 Then" +    // UTC-06:00 == -360
                   "\n  If [DST] > 0 Then" +
                   "\n    z = " + CentralST + ": If [" + countryCodeFieldName + "] = \"MEX\" Then z = " + CentralSTMexico +
                   "\n  Else" +
                   "\n    z = " + CentralAmericaST + ": If [" + countryCodeFieldName + "] = \"CAN\" Then z = " + CanadaCentralST +
                   "\n  End If" +
                   "\nElseIf utcOffset < -285 Then" +    // UTC-05:00 == -300
                   "\n  If [DST] > 0 Then" +
                   "\n    z = " + EasternST +
                   "\n  Else" +
                   "\n    z = " + SAPacificST + ": If [" + countryCodeFieldName + "] = \"MEX\" Then z = " + EasternSTMexico +
                   "\n  End If" +
                   "\nElseIf utcOffset < -255 Then" +    // UTC-04:30 == -270
                   "\n  z = " + VenezuelaST +
                   "\nElseIf utcOffset < -225 Then" +    // UTC-04:00 == -240
                   "\n  If [DST] > 0 Then" +
                   "\n    Select Case [" + countryCodeFieldName + "]" +
                   "\n      Case \"CAN\", \"GRL\": z = " + AtlanticST +
                   "\n      Case \"PRY\": z = " + ParaguayST +
                   "\n      Case Else: z = " + CentralBrazilianST +
                   "\n    End Select" +
                   "\n  Else" +
                   "\n    z = " + SAWesternST +
                   "\n  End If" +
                   "\nElseIf utcOffset < -195 Then" +    // UTC-03:30 == -210
                   "\n  z = " + NewfoundlandST +
                   "\nElseIf utcOffset < -150 Then" +    // UTC-03:00 == -180
                   "\n  If [DST] > 0 Then" +
                   "\n    Select Case [" + countryCodeFieldName + "]" +
                   "\n      Case \"SPM\", \"GRL\": z = " + GreenlandST +
                   "\n      Case Else: z = " + ESouthAmericaST +
                   "\n    End Select" +
                   "\n  Else" +
                   "\n    Select Case [" + countryCodeFieldName + "]" +
                   "\n      Case \"CHL\": z = " + PacificSAST +
                   "\n      Case \"ARG\": z = " + ArgentinaST +
                   "\n      Case \"URY\": z = " + MontevideoST +
                   "\n      Case Else: z = " + SAEasternST +
                   "\n    End Select" +
                   "\n  End If" +
                   "\nElseIf utcOffset < -90 Then" +    // UTC-02:00 == -120
                   "\n  z = " + UTCMinus02 + ": If [DST] > 0 Then z = " + MidAtlanticST +
                   "\nElseIf utcOffset < -30 Then" +    // UTC-01:00 == -60
                   "\n  z = " + CapeVerdeST + ": If [DST] > 0 Then z = " + AzoresST +
                   "\nElseIf utcOffset < 30 Then" +    // UTC == 0
                   "\n  If [DST] > 0 Then" +
                   "\n    z = " + GMTST + ": If [" + countryCodeFieldName + "] = \"MAR\" Then z = " + MoroccoST +
                   "\n  Else" +
                   "\n    Select Case [" + countryCodeFieldName + "]" +
                   "\n      Case \"ISL\", \"LBR\": z = " + GreenwichST +
                   "\n      Case Else: z = " + UTC +
                   "\n    End Select" +
                   "\n  End If" +
                   "\nElseIf utcOffset < 90 Then" +    // UTC+01:00 == 60
                   "\n  If [DST] > 0 Then" +
                   "\n    Select Case [" + countryCodeFieldName + "]" +
                   "\n      Case \"GIB\", \"ESP\", \"AND\", \"FRA\", \"MCO\", \"LUX\", \"BEL\", \"DNK\": z = " + RomanceST +
                   "\n      Case \"CZE\", \"SVK\", \"HUN\", \"SVN\", \"SRB\": z = " + CentralEuropeST +
                   "\n      Case \"HRV\", \"BIH\", \"MNE\", \"ALB\", \"MKD\", \"POL\": z = " + CentralEuropeanST +
                   "\n      Case \"NAM\": z = " + NamibiaST +
                   "\n      Case Else: z = " + WEuropeST +
                   "\n    End Select" +
                   "\n  Else" + 
                   "\n    z = " + WCentralAfricaST +
                   "\n  End If" +
                   "\nElseIf utcOffset < 150 Then" +    // UTC+02:00 == 120
                   "\n  If [DST] > 0 Then" +
                   "\n    Select Case [" + countryCodeFieldName + "]" +
                   "\n      Case \"ROU\", \"GRC\": z = " + GTBST +
                   "\n      Case \"TUR\": z = " + TurkeyST +
                   "\n      Case \"CYP\": z = " + EEuropeST +
                   "\n      Case \"EGY\": z = " + EgyptST +
                   "\n      Case \"ISR\": z = " + IsraelST +
                   "\n      Case \"LBN\": z = " + MiddleEastST +
                   "\n      Case \"SYR\": z = " + SyriaST +
                   "\n      Case \"JOR\": z = " + JordanST +
                   "\n      Case Else: z = " + FLEST +
                   "\n    End Select" +
                   "\n  Else" +
                   "\n    Select Case [" + countryCodeFieldName + "]" +
                   "\n      Case \"RUS\": z = " + KaliningradST +
                   "\n      Case \"LBY\": z = " + LibyaST +
                   "\n      Case Else: z = " + SouthAfricaST +
                   "\n    End Select" +
                   "\n  End If" +
                   "\nElseIf utcOffset < 195 Then" +    // UTC+03:00 == 180
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"BLR\": z = " + BelarusST +
                   "\n    Case \"RUS\": z = " + RussianST +
                   "\n    Case \"IRQ\": z = " + ArabicST +
                   "\n    Case \"SAU\", \"KWT\": z = " + ArabST +
                   "\n    Case Else: z = " + EAfricaST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 225 Then" +    // UTC+03:30 == 210
                   "\n  z = " + IranST +
                   "\nElseIf utcOffset < 255 Then" +    // UTC+04:00 == 240
                   "\n  If [DST] > 0 Then" +
                   "\n    z = " + AzerbaijanST +
                   "\n  Else" +
                   "\n    Select Case [" + countryCodeFieldName + "]" +
                   "\n      Case \"MUS\": z = " + MauritiusST +
                   "\n      Case \"GEO\": z = " + GeorgianST +
                   "\n      Case \"ARM\": z = " + CaucasusST +
                   "\n      Case \"RUS\": z = " + RussiaTimeZone3 +
                   "\n      Case Else: z = " + ArabianST +
                   "\n    End Select" +
                   "\n  End If" +
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
                   "\n    Case \"RUS\": z = " + NCentralAsiaST +
                   "\n    Case \"BGD\": z = " + BangladeshST +
                   "\n    Case Else: z = " + CentralAsiaST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 405 Then" +    // UTC+06:30 == 390
                   "\n  z = " + MyanmarST +
                   "\nElseIf utcOffset < 450 Then" +    // UTC+07:00 == 420
                   "\n  z = " + SEAsiaST + ": If [" + countryCodeFieldName + "] = \"RUS\" Then z = " + NorthAsiaST +
                   "\nElseIf utcOffset < 510 Then" +    // UTC+08:00 == 480
                   "\n  If [DST] > 0 Then" +
                   "\n    z = " + UlaanbaatarST +
                   "\n  Else" +
                   "\n    Select Case [" + countryCodeFieldName + "]" +
                   "\n      Case \"RUS\": z = " + NorthAsiaEastST +
                   "\n      Case \"TWN\": z = " + TaipeiST +
                   "\n      Case \"SGP\", \"MYS\": z = " + SingaporeST +
                   "\n      Case \"AUS\": z = " + WAustraliaST +
                   "\n      Case Else: z = " + ChinaST +
                   "\n    End Select" +
                   "\n  End If" +
                   "\nElseIf utcOffset < 555 Then" +    // UTC+09:00 == 540
                   "\n  Select Case [" + countryCodeFieldName + "]" +
                   "\n    Case \"RUS\": z = " + YakutskST +
                   "\n    Case \"JPN\": z = " + TokyoST +
                   "\n    Case Else: z = " + KoreaST +
                   "\n  End Select" +
                   "\nElseIf utcOffset < 585 Then" +    // UTC+09:30 == 570
                   "\n  z = " + AusCentralST + ": If [DST] > 0 Then z = " + CenAustraliaST +
                   "\nElseIf utcOffset < 630 Then" +    // UTC+10:00 == 600
                   "\n  If [DST] > 0 Then" +
                   "\n    z = " + AusEasternST +
                   "\n  Else" +
                   "\n    Select Case [" + countryCodeFieldName + "]" +
                   "\n      Case \"RUS\": z = " + VladivostokST +
                   "\n      Case \"AUS\": z = " + EAustraliaST +
                   "\n      Case Else: z = " + WestPacificST +
                   "\n    End Select" +
                   "\n  End If" +
                   "\nElseIf utcOffset < 690 Then" +    // UTC+11:00 == 660
                   "\n  z = " + CentralPacificST + ": If [" + countryCodeFieldName + "] = \"RUS\" Then z = " + RussiaTimeZone10 +
                   "\nElseIf utcOffset < 750 Then" +    // UTC+12:00 == 720
                   "\n  If [DST] > 0 Then" +
                   "\n    z = " + NewZealandST + ": If [" + countryCodeFieldName + "] = \"FJI\" Then z = " + FijiST +
                   "\n  Else" +
                   "\n    z = " + UTCPlus12 + ": If [" + countryCodeFieldName + "] = \"RUS\" Then z = " + RussiaTimeZone11 +
                   "\n  End If" +
                   "\nElseIf utcOffset < 810 Then" +    // UTC+13:00 == 780
                   "\n  z =  " + TongaST + ": If [DST] > 0 Then z = " + SamoaST +
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
