// Copyright 2021 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.TestUtilities
{
    public static class SampleKeyPairs
    {
        private const string KeyRsaPublic1024 = "-----BEGIN PUBLIC KEY-----" +
                                                "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDFZLu21dN5ZgFxkTj5PfN59xX5" +
                                                "94VJ7QJ5TqwXv1K0dt9P/CZkkb3Z6Wjz77xtFPfEUrF8BSKEpKkxcsUhENEZnyw6" +
                                                "W0kzG/9P+K+PS2La26cpHl236lKDKEynbupPZE98GmuitB+zgobhd59T22qBobn6" +
                                                "Vh913EoCh9mMVMkdnQIDAQAB" +
                                                "-----END PUBLIC KEY-----";

        private const string KeyRsaPrivate1024 = "-----BEGIN PRIVATE KEY-----" +
                                                 "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMVku7bV03lmAXGR" +
                                                 "OPk983n3Ffn3hUntAnlOrBe/UrR230/8JmSRvdnpaPPvvG0U98RSsXwFIoSkqTFy" +
                                                 "xSEQ0RmfLDpbSTMb/0/4r49LYtrbpykeXbfqUoMoTKdu6k9kT3waa6K0H7OChuF3" +
                                                 "n1PbaoGhufpWH3XcSgKH2YxUyR2dAgMBAAECgYBLLZJQkB96nN7v4d9RDcctLG2y" +
                                                 "RhL9lMbcbJoecT+Oe7eRPvdgViF4XO0b+rJI2TOEEfqGwW3kFtJZgtyRO1ZnQBVT" +
                                                 "O1Q0aOzL6TdLHg0FO+kCml71aQfZjVd8dmRZgfhYmS+yc7PQdDLMh/PTCxqtfgD0" +
                                                 "H7LjJwmE3mDpt7qqAQJBAPDNy3hurPOMf2PG/7OrWTIW19Cg4ncq8D8mW6O673H9" +
                                                 "VJkcHydbEz8LcOgDc92VYH2uOSNso858DlOHEwobgasCQQDR2aPicvpfPxKnQUvD" +
                                                 "cuDAEFU1GzHr9lENZ9YnZwGv1yIS8tc1ErI7kmgU20UW9q6sGlaGUaR+58KQfQ6z" +
                                                 "VKXXAkAUwKeYaXFeS+1um+fNhCbbujw/Lp5Vxs2No7CiG6onGL4Bs/q7WY3/EO1a" +
                                                 "EXIa1pTKQAmMlABJ+0cAy9NIO7ahAkEAjXiiKYnGDOwikStN/me16QWZzAGXeDJI" +
                                                 "ljcIguvIkVkBmbCpMRh3m/2puVXRkBehzli7ODZWJU/tNSd5/5/zZQJAGCDrhWWi" +
                                                 "zSBO8vs80Fsw25iN4oLmlrOVauc0QWwAzN3FmBCL3vj7pxy2Sfd2Au1PHVDSHkT/" +
                                                 "ACScStaAGdevPA==" +
                                                 "-----END PRIVATE KEY-----";

        private const string KeyRsaPublic2048 = "-----BEGIN PUBLIC KEY-----" +
                                                "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvUmLn8xYT34MP6/eQA3J" +
                                                "592m2XShxcC7/f+og20in7LGtRaCDcyObhiesKIYmpq9Xy+44ysyKQ/ZlajimiX+" +
                                                "F1AA/wOaxF+e7nKuzNzM/K9zCPvIi98hdC1/u1exCJdgZ9bHQhU3rEpG77QZqoH4" +
                                                "rbqWdTMzyUWNhQArNzIjGnoNlEoU7aGfiGz3fnZ/wTKsP9nLnUNSdqshDTKYo+g8" +
                                                "8jVgKiBB19Oijzs4Yfm3Ae9ABaoI2pYGSvigG/S2nWpYWjAQE/TT19V+bXdH1ZkQ" +
                                                "xRTEYcfB50jZ0rzG7kcMgwG71Z8/lHwdR66v1q8xeDTOFtPL0SrorTdm7cVay6Gj" +
                                                "EQIDAQAB" +
                                                "-----END PUBLIC KEY-----";

        private const string KeyRsaPrivate2048 = "-----BEGIN PRIVATE KEY-----" +
                                                 "MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC9SYufzFhPfgw/" +
                                                 "r95ADcnn3abZdKHFwLv9/6iDbSKfssa1FoINzI5uGJ6wohiamr1fL7jjKzIpD9mV" +
                                                 "qOKaJf4XUAD/A5rEX57ucq7M3Mz8r3MI+8iL3yF0LX+7V7EIl2Bn1sdCFTesSkbv" +
                                                 "tBmqgfitupZ1MzPJRY2FACs3MiMaeg2UShTtoZ+IbPd+dn/BMqw/2cudQ1J2qyEN" +
                                                 "Mpij6DzyNWAqIEHX06KPOzhh+bcB70AFqgjalgZK+KAb9LadalhaMBAT9NPX1X5t" +
                                                 "d0fVmRDFFMRhx8HnSNnSvMbuRwyDAbvVnz+UfB1Hrq/WrzF4NM4W08vRKuitN2bt" +
                                                 "xVrLoaMRAgMBAAECggEACd52Rhutw0NBw61nqB8v7APWaazXHi+LC+SvbyXhsd5s" +
                                                 "uXoAjxtKJDs/00zGfYNOMxnoCOwWBZfwPYljl50d0qBW8Waep7xROWY5NiJmi6bS" +
                                                 "0It7OsyWQeFcv10S0O0+rJQy/tTDlwzgIvFnGdcEnLRyCo8OeMSZ3JmiYBKDvbFJ" +
                                                 "tRg3cEuk11HeunZ+kYKPmm2tOT/XKR/0cxmsCDygMWFzRebDfTc1guSIcLWDGlVQ" +
                                                 "QBiFgA0FeIKId8yv2N45yprj0QE7kQFByU5ByFg1RIzA6fQHHg8fpJZcu7nrrju/" +
                                                 "WBggpNtFPVgSuKQOBf56uKBe5JnJAB98q8iqm+HuiwKBgQD59NgEpr3i2G1zZsVw" +
                                                 "2hwqd5t+52DAQbkZG0QAn7DY+C322ictzmSXDrMDbNZoc1Sw/OcxajWXdJ6RKy9f" +
                                                 "mY71mNLxrXEjh8Ho9Te6puG0sMgnzo1B61C1kLyVvx1DLMQ7LdBnmXs6LSZiKcPy" +
                                                 "HJ+ZyRgeyyNFy3n7lYBs9cjhgwKBgQDB3S14nZmZ4IXJeBxEd23/YRkgqJG/KF7z" +
                                                 "ruTQp7Gu3zi5AYDTgbVVLMJP5O3xsA7WeDkIyRbs/1EGFEA5UT6gjGkLnI7Gm+BP" +
                                                 "5Z72+se89AtWasrWeDYhuU2mDL8V4+IfYf4HsZZp09ikh7AH+aUCSlAr5lmYm56E" +
                                                 "d/90187o2wKBgFHg3pJHfJQ+iTvwQmUBTZCrtYgQiyTvYo7S26fIp8mrIoNmWscq" +
                                                 "gNDqw8EvedylSuzfK2yIeh2u2fJ7zvzl9GqHMTJxukoFQoPpL+Q4nl7uOeKwSp15" +
                                                 "U+rmCqCTBibnFzC7hTUqla8s8xHc1I8OyUk7Emej614FlWPQSU1oBfG5AoGBAL3t" +
                                                 "QHUggbFdY/UDdT4me19s8z8pptBObufx+j3pbIxUKLAnptyQAOUXWq8HK45S29aG" +
                                                 "JepTh+BcKjb4dAsza1XC+c7kbIRrhhEAdwKkojaeKNVa/qmrT+0uK8J4TmTVw1zX" +
                                                 "lhZXh3Laly5puK23iE98GptHq5N1MpG5Nk50d0NtAoGAa9bL4sglwRxGMvCh77hX" +
                                                 "qz9EfgZBsz9F1W55RQ7RMn6ZDb2wA9MNK3AHHyE/3ycJf8jiMygZY2lZ/JcbuD9I" +
                                                 "AqXd1NMj7nkkfe8QLaj2GS539+WjYqWoxUwvmTLxZiqCIr/WntTCEkFkm02cZFUz" +
                                                 "xPJgA0b3chq9LuMdCk7k04U=" +
                                                 "-----END PRIVATE KEY-----";

        private const string KeyRsaPublic3072 = "-----BEGIN PUBLIC KEY-----" +
                                                "MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEAmSYPuh+xpJJQW6P/3s9K" +
                                                "OPCAPD7NcSl74qOytuupdOaFxaxeZV0YX4AQCTNiLAI+Pl9ZIHeeSBtRkzykWnuu" +
                                                "JZ9IlQEjGDsCvD9ygc3kZFnJzmLWVGr76OEFLdQHVMBwRuuy9kRRc+PbgMd64O8b" +
                                                "sJYVNpc1FrBhSozZ9AxEme5dexUpNuEEFTHUhAAxVqH+btm7YOU7+A8lzuQD58bs" +
                                                "Vx+9f6VnWXqI6raNUAFnZDcjKNWl6MwctOWQEHLEmfH0Pu/Vth0cR8GYQ7omuEIg" +
                                                "PzvoYfUMe5bsubJTIYf6PUiZa9LuO8UlCcZU3UUElFD/M5cD3M+SVGF3GypWzUIT" +
                                                "ovV0tUVcVkpQ+YamYpaa/z7JX2jc3DtKoaz9Z+xn9wStXL9PFaL5vml5sk7CKnq2" +
                                                "mS3g3kogyoMb1pFdYotGchVjxMTidMksRm+Hf7jA27A8InqGACiz37Op/MWZDCKD" +
                                                "Q4zba0y8+ctCZfOQge5/MJ+1HeWHvL/5QWm0jIL+WRUhAgMBAAE=" +
                                                "-----END PUBLIC KEY-----";

        private const string KeyRsaPrivate3072 = "-----BEGIN PRIVATE KEY-----" +
                                                 "MIIG/AIBADANBgkqhkiG9w0BAQEFAASCBuYwggbiAgEAAoIBgQCZJg+6H7GkklBb" +
                                                 "o//ez0o48IA8Ps1xKXvio7K266l05oXFrF5lXRhfgBAJM2IsAj4+X1kgd55IG1GT" +
                                                 "PKRae64ln0iVASMYOwK8P3KBzeRkWcnOYtZUavvo4QUt1AdUwHBG67L2RFFz49uA" +
                                                 "x3rg7xuwlhU2lzUWsGFKjNn0DESZ7l17FSk24QQVMdSEADFWof5u2btg5Tv4DyXO" +
                                                 "5APnxuxXH71/pWdZeojqto1QAWdkNyMo1aXozBy05ZAQcsSZ8fQ+79W2HRxHwZhD" +
                                                 "uia4QiA/O+hh9Qx7luy5slMhh/o9SJlr0u47xSUJxlTdRQSUUP8zlwPcz5JUYXcb" +
                                                 "KlbNQhOi9XS1RVxWSlD5hqZilpr/PslfaNzcO0qhrP1n7Gf3BK1cv08Vovm+aXmy" +
                                                 "TsIqeraZLeDeSiDKgxvWkV1ii0ZyFWPExOJ0ySxGb4d/uMDbsDwieoYAKLPfs6n8" +
                                                 "xZkMIoNDjNtrTLz5y0Jl85CB7n8wn7Ud5Ye8v/lBabSMgv5ZFSECAwEAAQKCAYAt" +
                                                 "tQnZk0753n8kMpiRf41X3BNxp466GNb0B8Y1SLVNAeXn3q9XkkbNbdObY14H421/" +
                                                 "QQbBJWI0hA6/IkitBp+tc9H+QpYeS7Jfy5HZwsDI4HFV6vKrxDhFwy7ABDlh5oM7" +
                                                 "72l8jVw/+b/Puflm+4XomIphPhSmnmKTFOGRsD2jMVxt+R1RVyvYRYR3FvWitPtS" +
                                                 "SyJc412YBbFTg4LU4G41/G+akpt7PZJydqRLPfgFFV2leMoo5g4lQSRTfGVHysmT" +
                                                 "y3rCaajzDg27FEx0OL7UdhLer1wYYRWdT9lQ9kkVY9wrNf6G+VTFlIsSj1yXAtBk" +
                                                 "qL018BEijndLhU3Vnm104ZRsG9W9ySrZaX2FhAx17ssvg/qHnS3u5vFcOPIBoGWC" +
                                                 "+B/BvhnhWWSY+tWVLZIfPl4HQuQ2WivJb//X831JWRZYMbCFhh4NtirJO+bZf7b0" +
                                                 "hgCFIqWLLp8yWTZ5xx1Z7R4kd1uks4/XdC1Sr87Lu7Rcwd0Y+ALt04oh0PTG7ZcC" +
                                                 "gcEA0ItLpF9Ewi3nvHVglSc3piRv3elGnuiMK45ESc08M+4Dll8a50jTAIkjcMvN" +
                                                 "avTgHbYVk3xkGn3LZsea0+T9qtRvQJ5xsyleOk4Jy3NqNRN63cYJ9BhDEW+cCTrC" +
                                                 "TNlTdsaOP2+7Atz7g+Oh1DtnKJ8Lpcc2VhHGC+nH4u8KEAhkpQ5+YzakM0aiDF6R" +
                                                 "sQBIThMq2Zw33t/huBBVuVzJs9saRPTJvDPk7dwD7gi1MWWhCDaB1cWtxhsAhHeq" +
                                                 "zP7rAoHBALv/tpB06nUrtMEQ6UkQpY6kV45gTMDOrdlwKiJbgUPxuKd6TsC4jlW8" +
                                                 "YFZXk6AUCFGa77r5KJOxc/Y87kKdf63dnN1mjpRNBxrO83GtjAc+A8m3r5tERevi" +
                                                 "rNq1NEKugcRW8KZgo93HEEfh8NiQsHIg8LKooUnKIXNBA5qsJ5Z/Qt11jioNW33s" +
                                                 "pqkx6RmZiGlB1oTQmZW//pJvFgMJlflIzHut1ypcpowY37xfElyKKGRDP+YIr84t" +
                                                 "HN0iGUXxIwKBwFj2IxWCgn1nQcT2OXZHHYklYAdFPRgK0ci+zsjA4V6xuRwLhBmH" +
                                                 "ymMfHVw/xGhM/9IM29VnqfhXE07L9XNQ6xlVuAPT02L/UbADnFAK8xKjNbWnhpV3" +
                                                 "SB0HBIQ3aa2Iw/8WIpZTHm7RQAX6NA2qLY55kmlsuvQqbtakKt3W5O8D9ZMnxKik" +
                                                 "JZWuGvC14uaj3TRZHt1ns7nCvbJcXYVOXMj5vZIO7oP3i0AgrBh95HWnCfPL9MTx" +
                                                 "p/iriiP4PIdocwKBwG+kiL86ny1b+iiZKWCZgSe3UsObTplFY5p38J2cp6Q4vQbA" +
                                                 "LFpofyZNCwzbTzDGFLaZgvoPEti6jfnR71AiBfuzWn9kcxGAuNJjydBdVoXKfydg" +
                                                 "bOmQ3tEZOLtc1p8u0KNPWfQD+ewvVezKMWP6cL4l76q5V6bhYYH3PvOwfoXyJzwq" +
                                                 "nnU8n3OlgMeDe0EXmxme3oza8AotDTnavECrhaOXZs+fyeI/SSxzbRKJhvbrmNcJ" +
                                                 "1L1/tR+ETNrJcCbH7wKBwCQ2darkzREjCficN8VdYacoWfa2SsJLo+mk6Nw/E2OD" +
                                                 "LGG8m3VXEi4wxTCxZV0t7HoRR4NvB/Co3ag1dxxHu86cVmQxtWGvujQ4XdfBAUHo" +
                                                 "cIEq73PMyHurNMZRyoa/B5rCcrDQVm7NW+p23GS9VoVsdnWqynk/OVG32zSe3arz" +
                                                 "XN4jazgcSBhhKL/tl9LInGYXNjN2M8icSwntvWKD2V337UfpP82/gqbjs1MDOd0A" +
                                                 "tMaBxdHK1Tub2v2ww5i80w==" +
                                                 "-----END PRIVATE KEY-----";

        private const string KeyRsaPublic4096 = "-----BEGIN PUBLIC KEY-----" +
                                                "MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAuBEzOgSXmuIY5UR0NdHr" +
                                                "MVxVZ0PC/4qqc4X6NiKxpDdNxmFhDXUMBcNFS5t7d4wayjeLgYbFqQiRPkFNE6Qn" +
                                                "CuqtEtp4K+GASaHY6zSRd17fVnpMBetCBZKvfD28I/QOQSpEztXISdg+g1JCHHLx" +
                                                "l1UBVhEx49ZgYNchF9CLs5IBUZEvUl3EJb+6YCioVr6Lv2gJWUY7CAQqVszmEKm2" +
                                                "79xfsaxFBVQ5cAdTj09QoDKgZ7sSvL132E5W2mewmkArqWb4TGEhw2cni6jlSs+e" +
                                                "3mDRb3iuc6u8UXEun+O58vsF1eAVXr8CzDFQQSypNO/rCDi+GMhSezfOyve9KD1i" +
                                                "YVvIVR7SlRGcf9XnjASgTmpa1/+L0T6bKlFuZxUaFzswNzFaYR5V00PUiOxSDqZd" +
                                                "nluYDZkSCv+DRZoXVAsU59jACnyjO8hi63V+BgZ2KFgIevRuiL/RJQ4d3kx0YYyP" +
                                                "JMnbOBz0LTygXY5cpjslVVbMUoYJsm3AapdZj86DQJlX6mQWVodpvdLAsJQETjhV" +
                                                "vkwdG2vCVeIFVvRqVgMeOSSD+M2pQ8PgDH4aQ3WPLtmJzjLvI9RC2AcJKxJyIZ1o" +
                                                "tT4KxjkRd8tSpLVq5WZhT0dU3EI8P5K0cq808ESOj2AQitbi1CPbBt3OCKSBxtjI" +
                                                "18o/8NtMmeRrlC2Qt/2ZEJcCAwEAAQ==" +
                                                "-----END PUBLIC KEY-----";

        private const string KeyRsaPrivate4096 = "-----BEGIN PRIVATE KEY-----" +
                                                 "MIIJQQIBADANBgkqhkiG9w0BAQEFAASCCSswggknAgEAAoICAQC4ETM6BJea4hjl" +
                                                 "RHQ10esxXFVnQ8L/iqpzhfo2IrGkN03GYWENdQwFw0VLm3t3jBrKN4uBhsWpCJE+" +
                                                 "QU0TpCcK6q0S2ngr4YBJodjrNJF3Xt9WekwF60IFkq98Pbwj9A5BKkTO1chJ2D6D" +
                                                 "UkIccvGXVQFWETHj1mBg1yEX0IuzkgFRkS9SXcQlv7pgKKhWvou/aAlZRjsIBCpW" +
                                                 "zOYQqbbv3F+xrEUFVDlwB1OPT1CgMqBnuxK8vXfYTlbaZ7CaQCupZvhMYSHDZyeL" +
                                                 "qOVKz57eYNFveK5zq7xRcS6f47ny+wXV4BVevwLMMVBBLKk07+sIOL4YyFJ7N87K" +
                                                 "970oPWJhW8hVHtKVEZx/1eeMBKBOalrX/4vRPpsqUW5nFRoXOzA3MVphHlXTQ9SI" +
                                                 "7FIOpl2eW5gNmRIK/4NFmhdUCxTn2MAKfKM7yGLrdX4GBnYoWAh69G6Iv9ElDh3e" +
                                                 "THRhjI8kyds4HPQtPKBdjlymOyVVVsxShgmybcBql1mPzoNAmVfqZBZWh2m90sCw" +
                                                 "lAROOFW+TB0ba8JV4gVW9GpWAx45JIP4zalDw+AMfhpDdY8u2YnOMu8j1ELYBwkr" +
                                                 "EnIhnWi1PgrGORF3y1KktWrlZmFPR1TcQjw/krRyrzTwRI6PYBCK1uLUI9sG3c4I" +
                                                 "pIHG2MjXyj/w20yZ5GuULZC3/ZkQlwIDAQABAoICABBO9DmR8JcreZM1E0Rorflf" +
                                                 "Br4Ygq1x7S10U5/6R1nKZwMXQhCw+MWF8JzRtzwqikq1mZAUKQbGy7+dUr8iJ/qc" +
                                                 "O+ttg4CeNGfRuQHV7YS+P8GBiNZsnecXTdV1m50zFK3XZeaBihzPPD98YuXyyGiY" +
                                                 "T0vMSDeGMsUOkOlzc8oPbjutrM8UBoeY5JV8F+lfoDG8Sp/BswQNAm25+v32jrVs" +
                                                 "n00c4bZAKSEKge/Ma9NVyPqH2HgnVvf8sOgdXPtXVi1maMRNRgvySgGvKQNA2mc1" +
                                                 "/Guik92LYl5A54wJEqk7O0aHQFcQ5QzC2pGu64SiNGVmsAkYFQUpahJ3pfQ4kOYl" +
                                                 "qQVIfIRtvfiktSwUVLPx1SUXTHRxH0zTctOjTz1+aHJhlwI1CB/zZIThE+Y2d+gZ" +
                                                 "q2Z08pbqVfRCuTY0ttuBNyLoNv0BZfZRh1xqCQve0cZgk95L9IBzzj4XjqpTMy/E" +
                                                 "risu69v8Z0/uT78mb2PbWXwvCdF2lg/B8H55kWqcNHj3JRNqoJwOPQVKpQlJRHx0" +
                                                 "NAIrtxSBkRNEPdHmxyZA5SFK7/rQ6Rm0hQNkbAkk+GPqE0yqfPxj6UvMTznaxvQ8" +
                                                 "CBoB4t7tzZp4VLjcUaMqYK1nBsfcCrv31YTuuVNpcuUpXBxSk8JDUqRmroN2/e1+" +
                                                 "8GYgWrx8ZT3LL2Q1TVCRAoIBAQD9flRMfBJyE7pM4IDPrY+iDR/Ywoy5MXgx/X/8" +
                                                 "e2hr0K62jXGUVdF+cveDi6rJFGrsnKUROzA0iuRAyTAomCdANN5SHhjiyWqe//iN" +
                                                 "Dxm3P/RBpmbPUjMsK3R/l/3iVvGceulnDcWYo0Ovn4OwBqQosTU+dTyFZ3IqE3Y2" +
                                                 "d9NEIrbsobXedlaNvAL83HT3GckYHOiNiqOIuaCCUCbCOmUSsoKh5kTB9ojZBsWL" +
                                                 "EFQZYe447kNxacY/D2Y/Z458+GdW+zIsBVTdZgrB19yME0rX+Pm8rNgL0wBOvQMG" +
                                                 "1ijJ7QetdBsqDZEPwcegrSnKbNf52M9fnqwWtwRoBIpns10ZAoIBAQC54yGeQu4i" +
                                                 "Cvjk3tYYG1aCR4qmqSXfUbbeccxdOc0UHhAypZ5NGaYtjHHQ7chuFSA2We+MGtyW" +
                                                 "C1IOSEVS+pY3K2rVp94lolVT+vSt71PnmFagjcrY8bs+i3AGgw4YtRWVJNkQE0OJ" +
                                                 "627+Ya91Ia1dYR6vWoZX5JTJ2NdPRPQpPWcVdJpTDI5kFoTiwHB/arQAkLOv4Quv" +
                                                 "qhynVH0fEoiZJFRaR6iDGhAgDKB6sA7Tqb6yPJv0IehlZoLKG6oqtfQht9DE6FYl" +
                                                 "tQDj9XTbbdhPsXXHchdQBuT9IEXdSfs5nsgBwlg3h9pG/BqDFtXu+B52AEnuaR+v" +
                                                 "/AvFkNxImOEvAoIBABGAoHdrdaaUwB9AvQQZ2rn4qANCY48B4GerNiQLrUkMbpPC" +
                                                 "Ll5skntlmrtlcFRT6ZIOusL20DxAfsQOYBndb5BaViNbWqKF/6ucxt+OdFsXuliy" +
                                                 "EZUs+sWI9pE8wFXZZPNF9UmdRNBmLW052VDVFI6OtbtrQtN/Mf2/vEDEgzzIHNM3" +
                                                 "0yPaDd3ZZmdpHVZWXHEixdfIA8ST9IYq3JI6j/H7i1N8X7D4wbgiZI9WgEgEX/tk" +
                                                 "UBnLkNmXyZqFHux4BkKWM3+gmpxyyDlcGyk8x8UjtrKVSJGAbxwApu3Y6ZYPnKEY" +
                                                 "TCvaJfLtkUgBzMniPANPOfpDLWSgHFjGP3wrgTkCggEAUgZm1EYmfIEo+R1XjcWq" +
                                                 "c8yL4yTqoFOXhSrkChMyanklnqO0acMysBC0PIRgmCrcTv96k/Faex89sy2y4X3Q" +
                                                 "AUI4X1U20paCXo9znrjn5l8zgp9u7jIk9OFkqor0EnT9tBVRbyWA/QAVt0x1txMI" +
                                                 "RBdSCgDBHVGxUixMPh9oOjZtIWuVmaYFwyaotsJCIgd8rG8tyyNcG8TN5gyDNc3g" +
                                                 "1urQChJqyocarHnF6r17nWzeyBm0m5LG0M/eUL7KZRRrSOGqzujS8sqfPPgX+6fJ" +
                                                 "9siQ91Rh8x9HtmaiTZaStAdbrGMMuFxLNl2SeVv/RPbZwio4dWqP4AExVJmqiqJj" +
                                                 "YwKCAQBB5mPlC6fKCzKBDCPLwlCB38OrFv7CANpGFlRoAc8JefSwN07Yc5hTNB5h" +
                                                 "F7Ck76eiJL2qi5JsR5C1IBPZJ093iurrXHsp9wdCnBbNvoH77i2lmOzfPAp+0ygb" +
                                                 "XuKCTC5QWDV9Xmx800ZhDup5i5WjtJfEUWpdlDR/+SyJV6EnudutRanf4UINAob7" +
                                                 "ljYNdPc4oqwfI4vzqrlJL4nRBG4Z6xmiwb4s2+RvrAYOMJ8M3M2DbAcoJmGddyYh" +
                                                 "asEVYcdQMyBDvBoRdtvChSbvnsUFe3wvN9korHHDsy0Etf3nE36RVwcNeFxXd/WM" +
                                                 "Z3McbfcguzFzNcp74vKiOTaMhmy/" +
                                                 "-----END PRIVATE KEY-----";

        private const string CertRsa1024 = "-----BEGIN CERTIFICATE-----" +
                                        "MIICDjCCAXcCFGwJuW73tplobguDN0FmsjxtKEHvMA0GCSqGSIb3DQEBCwUAMEYx" +
                                        "CzAJBgNVBAYTAlNFMRIwEAYDVQQIDAlTdG9ja2hvbG0xEjAQBgNVBAcMCVN0b2Nr" +
                                        "aG9sbTEPMA0GA1UECgwGWXViaWNvMB4XDTI0MDYwNDEwMTkzMloXDTI0MDYwNDEw" +
                                        "MTkzMlowRjELMAkGA1UEBhMCU0UxEjAQBgNVBAgMCVN0b2NraG9sbTESMBAGA1UE" +
                                        "BwwJU3RvY2tob2xtMQ8wDQYDVQQKDAZZdWJpY28wgZ8wDQYJKoZIhvcNAQEBBQAD" +
                                        "gY0AMIGJAoGBAMVku7bV03lmAXGROPk983n3Ffn3hUntAnlOrBe/UrR230/8JmSR" +
                                        "vdnpaPPvvG0U98RSsXwFIoSkqTFyxSEQ0RmfLDpbSTMb/0/4r49LYtrbpykeXbfq" +
                                        "UoMoTKdu6k9kT3waa6K0H7OChuF3n1PbaoGhufpWH3XcSgKH2YxUyR2dAgMBAAEw" +
                                        "DQYJKoZIhvcNAQELBQADgYEAmUzNnXcNgYDMQ+XVsOQzhuiwVZOCEGQue0s5hDFC" +
                                        "Os6y1/cvHRRefWsPuxTIqNgV6VmZInyoVHoep6MWfptQYqseMNI1/WJCUr2OnC/d" +
                                        "GYBQTDCxdlboL4qSk9BonzEMP9D+Gg+h2hrfej9Al1/UwRHcp14cgATwtXZSOlNI" +
                                        "QiI=" +
                                        "-----END CERTIFICATE-----";

        private const string CertRsa2048 =
                                        "-----BEGIN CERTIFICATE-----" +
                                        "MIIDEzCCAfsCFC+WImHvRezTDqI59Ffg3ac9++ahMA0GCSqGSIb3DQEBCwUAMEYx" +
                                        "CzAJBgNVBAYTAlNFMRIwEAYDVQQIDAlTdG9ja2hvbG0xEjAQBgNVBAcMCVN0b2Nr" +
                                        "aG9sbTEPMA0GA1UECgwGWXViaWNvMB4XDTI0MDYwNDEwMTk1MFoXDTI0MDYwNDEw" +
                                        "MTk1MFowRjELMAkGA1UEBhMCU0UxEjAQBgNVBAgMCVN0b2NraG9sbTESMBAGA1UE" +
                                        "BwwJU3RvY2tob2xtMQ8wDQYDVQQKDAZZdWJpY28wggEiMA0GCSqGSIb3DQEBAQUA" +
                                        "A4IBDwAwggEKAoIBAQC9SYufzFhPfgw/r95ADcnn3abZdKHFwLv9/6iDbSKfssa1" +
                                        "FoINzI5uGJ6wohiamr1fL7jjKzIpD9mVqOKaJf4XUAD/A5rEX57ucq7M3Mz8r3MI" +
                                        "+8iL3yF0LX+7V7EIl2Bn1sdCFTesSkbvtBmqgfitupZ1MzPJRY2FACs3MiMaeg2U" +
                                        "ShTtoZ+IbPd+dn/BMqw/2cudQ1J2qyENMpij6DzyNWAqIEHX06KPOzhh+bcB70AF" +
                                        "qgjalgZK+KAb9LadalhaMBAT9NPX1X5td0fVmRDFFMRhx8HnSNnSvMbuRwyDAbvV" +
                                        "nz+UfB1Hrq/WrzF4NM4W08vRKuitN2btxVrLoaMRAgMBAAEwDQYJKoZIhvcNAQEL" +
                                        "BQADggEBAKv4tbsNVdNuM+EZwew9aMO/SjDMG3dzJlPAoB1t56nhRPpu1WKl7Ws/" +
                                        "s7RETf9X1AUR3vdHroGh9IIBQi8kXXPnTgI7piXCHXJvf46EXkhItiZ2YG/8acG2" +
                                        "sJcSVLBYkYKlUb8Y3oKdHtaDfHrXM9rTkwIHVcu9KOq4Dv7azsts+D1t7xGoXJKo" +
                                        "8thBpg6+SPQs86N6V0JU5l1nC2C1NKRQtgrxaURf4gPKpPDepN8Y0rlevbs8X2Xp" +
                                        "sBqR5dH4k9xwj8/GZURuNq36fwyyZAgXoc9de2SWjYwGmyMwQOE2aGO5pXL8LOT9" +
                                        "R480m6XPlN+Bp9jEYU5PWfnzcztjpkA=" +
                                        "-----END CERTIFICATE-----";

        private const string CertRsa3072 = "-----BEGIN CERTIFICATE-----" +
                                        "MIIEbzCCAtcCFDaL3lZCak/LZYFzH9X3Ib3CpmmLMA0GCSqGSIb3DQEBCwUAMHQx" +
                                        "CzAJBgNVBAYTAlNFMQ4wDAYDVQQIDAVTdGhsbTEMMAoGA1UEBwwDU3dlMQ4wDAYD" +
                                        "VQQKDAVZdWJpYzEPMA0GA1UECwwGWXViaWNvMQ8wDQYDVQQDDAZZdWJpY28xFTAT" +
                                        "BgkqhkiG9w0BCQEWBnl1YmljbzAeFw0yNDA1MzExMTM0NTFaFw0yNDA1MzExMTM0" +
                                        "NTFaMHQxCzAJBgNVBAYTAlNFMQ4wDAYDVQQIDAVTdGhsbTEMMAoGA1UEBwwDU3dl" +
                                        "MQ4wDAYDVQQKDAVZdWJpYzEPMA0GA1UECwwGWXViaWNvMQ8wDQYDVQQDDAZZdWJp" +
                                        "Y28xFTATBgkqhkiG9w0BCQEWBnl1YmljbzCCAaIwDQYJKoZIhvcNAQEBBQADggGP" +
                                        "ADCCAYoCggGBAJkmD7ofsaSSUFuj/97PSjjwgDw+zXEpe+KjsrbrqXTmhcWsXmVd" +
                                        "GF+AEAkzYiwCPj5fWSB3nkgbUZM8pFp7riWfSJUBIxg7Arw/coHN5GRZyc5i1lRq" +
                                        "++jhBS3UB1TAcEbrsvZEUXPj24DHeuDvG7CWFTaXNRawYUqM2fQMRJnuXXsVKTbh" +
                                        "BBUx1IQAMVah/m7Zu2DlO/gPJc7kA+fG7FcfvX+lZ1l6iOq2jVABZ2Q3IyjVpejM" +
                                        "HLTlkBByxJnx9D7v1bYdHEfBmEO6JrhCID876GH1DHuW7LmyUyGH+j1ImWvS7jvF" +
                                        "JQnGVN1FBJRQ/zOXA9zPklRhdxsqVs1CE6L1dLVFXFZKUPmGpmKWmv8+yV9o3Nw7" +
                                        "SqGs/WfsZ/cErVy/TxWi+b5pebJOwip6tpkt4N5KIMqDG9aRXWKLRnIVY8TE4nTJ" +
                                        "LEZvh3+4wNuwPCJ6hgAos9+zqfzFmQwig0OM22tMvPnLQmXzkIHufzCftR3lh7y/" +
                                        "+UFptIyC/lkVIQIDAQABMA0GCSqGSIb3DQEBCwUAA4IBgQBTiQTbmwu1wM//Qvd0" +
                                        "WE0uxaYkDI25cyu5eH4dCo9QUDo2xk/CMuNt4xAk7CLBEqhzzMDdnD5SQKNN1Rgk" +
                                        "pIoZfyzbrsFd2JlcDIm8ZAxYITAPp9cDZqrM9mFjaWGFSenay3yqP5JcT68FuEwp" +
                                        "RtbVM2fWTyRJvjcfCcZMdefFhOmcdaoPy/EjcR5iRvBkyYfengAShPsEzeWC51jp" +
                                        "JkjNISJdZ3CnCk8XQv333RC9S0iLKTEA3zZOIQtJRBbrdfg6tM7w8eNsID6usa0L" +
                                        "ALD8vMmZCXrI1DXQ/yJRL/gZwui7HrMI8xw2KBUvn84sSg7Vi8RZdBF164N0XoUR" +
                                        "sNe8CQFIpyrwCYJROjU0GbnNTLTSBsZ6WLFJI8LhkzJRCjB/vYHJjihmaIBacmCK" +
                                        "beg7amRY9+lz+dfh89IqfHM6Y2N2dubBz37l2YZwZ3rrmoST4fLbuj0UU/mwRano" +
                                        "pPPfz8xaiBIh2lHu7FxUp5jjmsQ1QdCgr7wYOwjl9YQd3XQ=" +
                                        "-----END CERTIFICATE-----";

        private const string CertRsa4096 = "-----BEGIN CERTIFICATE-----" +
                                        "MIIFbzCCA1cCFGYdlOTmPdtfwrih5P9gZlJDO7SUMA0GCSqGSIb3DQEBCwUAMHQx" +
                                        "CzAJBgNVBAYTAlNFMQ4wDAYDVQQIDAVTdGhsbTEMMAoGA1UEBwwDU3dlMQ4wDAYD" +
                                        "VQQKDAVZdWJpYzEPMA0GA1UECwwGWXViaWNvMQ8wDQYDVQQDDAZZdWJpY28xFTAT" +
                                        "BgkqhkiG9w0BCQEWBnl1YmljbzAeFw0yNDA1MzExMjQxMjZaFw0yNDA1MzExMjQx" +
                                        "MjZaMHQxCzAJBgNVBAYTAlNFMQ4wDAYDVQQIDAVTdGhsbTEMMAoGA1UEBwwDU3dl" +
                                        "MQ4wDAYDVQQKDAVZdWJpYzEPMA0GA1UECwwGWXViaWNvMQ8wDQYDVQQDDAZZdWJp" +
                                        "Y28xFTATBgkqhkiG9w0BCQEWBnl1YmljbzCCAiIwDQYJKoZIhvcNAQEBBQADggIP" +
                                        "ADCCAgoCggIBALgRMzoEl5riGOVEdDXR6zFcVWdDwv+KqnOF+jYisaQ3TcZhYQ11" +
                                        "DAXDRUube3eMGso3i4GGxakIkT5BTROkJwrqrRLaeCvhgEmh2Os0kXde31Z6TAXr" +
                                        "QgWSr3w9vCP0DkEqRM7VyEnYPoNSQhxy8ZdVAVYRMePWYGDXIRfQi7OSAVGRL1Jd" +
                                        "xCW/umAoqFa+i79oCVlGOwgEKlbM5hCptu/cX7GsRQVUOXAHU49PUKAyoGe7Ery9" +
                                        "d9hOVtpnsJpAK6lm+ExhIcNnJ4uo5UrPnt5g0W94rnOrvFFxLp/jufL7BdXgFV6/" +
                                        "AswxUEEsqTTv6wg4vhjIUns3zsr3vSg9YmFbyFUe0pURnH/V54wEoE5qWtf/i9E+" +
                                        "mypRbmcVGhc7MDcxWmEeVdND1IjsUg6mXZ5bmA2ZEgr/g0WaF1QLFOfYwAp8ozvI" +
                                        "Yut1fgYGdihYCHr0boi/0SUOHd5MdGGMjyTJ2zgc9C08oF2OXKY7JVVWzFKGCbJt" +
                                        "wGqXWY/Og0CZV+pkFlaHab3SwLCUBE44Vb5MHRtrwlXiBVb0alYDHjkkg/jNqUPD" +
                                        "4Ax+GkN1jy7Zic4y7yPUQtgHCSsSciGdaLU+CsY5EXfLUqS1auVmYU9HVNxCPD+S" +
                                        "tHKvNPBEjo9gEIrW4tQj2wbdzgikgcbYyNfKP/DbTJnka5QtkLf9mRCXAgMBAAEw" +
                                        "DQYJKoZIhvcNAQELBQADggIBAJpqIBlRkPwN3woPoGFzGCesLWfBHxDbD2Kk+1Tm" +
                                        "xtBh+flC3hzuiWHkOO6KWcSfvPq1IlJX3JZhh+p9eF/Qt3UIUliqrFqv4SgDPmbM" +
                                        "d+w3ZAe+VV1NnKcGOGdLweEkldxeauBU2QuUi1D/j13JJbJIVfraAf9zxyhdkzKr" +
                                        "T3qj8ebZptOnmGqAE7afBIFEw65jF00lQAdrNVdea1WHlbjGWYUmMbuGGAWuVuJI" +
                                        "U7FckbLNHcVeAt8Cms8SgoiEBr5YxWnWko/EZzClLxlxS7YL4nnZ/WZj3J30FLbc" +
                                        "L2miduxeZGTDUjxBXlmVPP36ukFcR9HmQ/ydSTlGyAGqlbwMx/GrIb7jQmcfGcjN" +
                                        "zoP7Dsia4z+coROmB2TMP/nfozDyx5V60DcG4kpT5HSs7MlzUhZm6ydp3gQqRGZZ" +
                                        "DKjd7W8ZxvOG6Bl0Lh3qIPNfOPjEIOestA1Irzs0rVV8zBrVJTrctNtahBiZpXyq" +
                                        "CzfrswHU1tnQgcqPrFfQQ4jR0Ovz1DdArFK0J5mwW0TI3DZU8c2UE1zNtwUG0Al/" +
                                        "J9sZSq2jp5tX0sZJoPYiMBW0VN76YyqsjfHcyxNTn3BX+eT7jdVx4fLkrh68YPeO" +
                                        "xthHfyKfPt5uM1X0v+lnLZgo+0Ulra9Wvgccj1J3r9g5paT2WIEKXLHLI1BeO6MC" +
                                        "I3oS" +
                                        "-----END CERTIFICATE-----";

        private const string KeyEccPublicP256 = "-----BEGIN PUBLIC KEY-----" +
                                                "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEgE3OZMV7eQ+b+qFSDdQ5TCQ9pgg2" +
                                                "9dxmUGX87owhbBHgU+RrlNd5Xi/qfcjl0qD1yJuM0vg5m4NjqT3zx6q0+Q==" +
                                                "-----END PUBLIC KEY-----";

        private const string KeyEccPrivateP256 = "-----BEGIN PRIVATE KEY-----" +
                                                 "MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgIEdISnMgVtpVb28B" +
                                                 "8mxlrQ7eHtw8WbtvuV6BaK6jm/yhRANCAASATc5kxXt5D5v6oVIN1DlMJD2mCDb1" +
                                                 "3GZQZfzujCFsEeBT5GuU13leL+p9yOXSoPXIm4zS+Dmbg2OpPfPHqrT5" +
                                                 "-----END PRIVATE KEY-----";

        private const string KeyEccPublicP384 = "-----BEGIN PUBLIC KEY-----" +
                                                "MHYwEAYHKoZIzj0CAQYFK4EEACIDYgAEDCbayUu6FFcW/aH103hlh0cw8s/gxM5L" +
                                                "TTT99sNZZOFtcSWEe0dFI7oQ4kZHO8AuHt1FjfsB1Bi7aHXrjC558b6eNKbEd5P0" +
                                                "lVGwp+lD9EBRfyTk7/Xxran3HARlTR7f" +
                                                "-----END PUBLIC KEY-----";

        private const string KeyEccPrivateP384 = "-----BEGIN PRIVATE KEY-----" +
                                                 "MIG2AgEAMBAGByqGSM49AgEGBSuBBAAiBIGeMIGbAgEBBDAKroxlCShQ6sz89gMc" +
                                                 "K09DIyI8kxaRZt9GlCMmbbmVpGhqhQLvwVdowQkA0xQ9A3+hZANiAAQMJtrJS7oU" +
                                                 "Vxb9ofXTeGWHRzDyz+DEzktNNP32w1lk4W1xJYR7R0UjuhDiRkc7wC4e3UWN+wHU" +
                                                 "GLtodeuMLnnxvp40psR3k/SVUbCn6UP0QFF/JOTv9fGtqfccBGVNHt8=" +
                                                 "-----END PRIVATE KEY-----";

        // Get a matching key pair. Return the keys as strings, the PEM key data.
        public static void
            GetPemKeyPair(
                PivAlgorithm algorithm, out string publicKey,
                out string privateKey) 
        {
            switch (algorithm)
            {
                case PivAlgorithm.EccP256:
                    publicKey = KeyEccPublicP256;
                    privateKey = KeyEccPrivateP256;

                    break;

                case PivAlgorithm.EccP384:
                    publicKey = KeyEccPublicP384;
                    privateKey = KeyEccPrivateP384;

                    break;

                case PivAlgorithm.Rsa1024:
                    publicKey = KeyRsaPublic1024;
                    privateKey = KeyRsaPrivate1024;

                    break;

                case PivAlgorithm.Rsa2048:
                    publicKey = KeyRsaPublic2048;
                    privateKey = KeyRsaPrivate2048;

                    break;
                
                case PivAlgorithm.Rsa3072:
                    publicKey = KeyRsaPublic3072;
                    privateKey = KeyRsaPrivate3072;

                    break;

                case PivAlgorithm.Rsa4096:
                    publicKey = KeyRsaPublic4096;
                    privateKey = KeyRsaPrivate4096;

                    break;
                default: throw new ArgumentException("No key / cert mapped", nameof(algorithm));
            }
        }


        // This gets a private key with a cert that contains the public key
        // partner. This can only get one key and cert (the same one each time)
        public static bool GetMatchingKeyAndCert(
            PivAlgorithm algorithm,
            out X509Certificate2 cert, 
            out PivPrivateKey privateKey)
        {
            cert = GetCert(algorithm);
            privateKey = GetPivPrivateKey(algorithm);
            return true;
        }
        
        // Return the PEM of a private key and a certificate for that key.
        // The algorithm arg specifies the algorithm of the private key and the
        // subject public key. For all the certs in this method, the signing
        // algorithm (the algorithm used by the entity that signed the cert) is
        // RSA (2048-bit key) with SHA-256.
        // If the validAttest arg is true, the cert will be version 3  and
        // contain the extension BasicConstraints (it will be a CA cert). If
        // false, the cert will be version 1 (no extensions) and will be a leaf
        // cert. Note that a 1024-bit key is not allowed to be an attestation
        // key, so the key and cert returned will be invalid for attestation no
        // matter what (but it will be god for a negative test).
        // These keys and certs can be used for anything, they don't have to be
        // used in attestation. That is, if you are not dealing with attestation,
        // and you need a key and cert, these might work.
        public static bool GetKeyAndCertPem( 
            PivAlgorithm algorithm,
            bool validAttest,
            out string cert,
            out string privateKey)
        {
            switch (algorithm)
            {
                default:
                    cert = "nocert";
                    privateKey = "nokey";
                    return false;

                case PivAlgorithm.Rsa1024: //Todo might have to regen cert with correct CA extensions
                    if (validAttest)
                    {
                        cert =
                            "-----BEGIN CERTIFICATE-----" +
                            "MIIC2zCCAcOgAwIBAgICBRcwDQYJKoZIhvcNAQELBQAwZDELMAkGA1UEBhMCVVMx" +
                            "CzAJBgNVBAgMAkNBMRcwFQYDVQQHDA5QYWxvIEFsdG8gUm9vdDESMBAGA1UECgwJ" +
                            "RmFrZSBSb290MRswGQYDVQQDDBJGYWtlIFJvb3QgUlNBIDIwNDgwHhcNMjEwNDE2" +
                            "MTc0MjEwWhcNMzEwNDA0MTc0MjEwWjBpMQswCQYDVQQGEwJVUzELMAkGA1UECAwC" +
                            "Q0ExEjAQBgNVBAcMCVBhbG8gQWx0bzEZMBcGA1UECgwQRmFrZSBBdHRlc3RhdGlv" +
                            "bjEeMBwGA1UEAwwVRmFrZSBBdHRlc3RhdGlvbiAxMDI0MIGfMA0GCSqGSIb3DQEB" +
                            "AQUAA4GNADCBiQKBgQCxv2vOcMt+rn+EZsKSP5UlIuG3LnhiWKHaVEvfQJwXiOhx" +
                            "RCtowX9J4ij14ZB5zvwrmoxfMn2uu4ZB8QvuztdNiSKZKenqx5uqV4ki9gns1rem" +
                            "cuwGo/C2GvDY/Bpd4pDgWzARvc3QPMVVKic2JCRb5/pSUkvURwGtOC+51NPIrwID" +
                            "AQABoxYwFDASBgNVHRMBAf8ECDAGAQH/AgEBMA0GCSqGSIb3DQEBCwUAA4IBAQCr" +
                            "L3VVxhYuGTyviLmcKxhIrQWRZgBfp+09bWtpoAjwYUGLjhaUpzh89N6ySUrNRPpT" +
                            "vnN4aNwEPao6QafED1DLOreJSEbKeVAG0/NXT1LSioibmOBgqN9t9Kv4GqQjelgV" +
                            "GNx1iZk2jLVqGPocpmoZbDXkIfaLB39Opm7yJyFKL5A6sTInTmysOagSO/zTI/3N" +
                            "etoTT7KdwM99x6etAz+u8GAJqJ3Tdmp0RKWxM6V5FNXXRoDa1TxPLAzxOr1S5Bpb" +
                            "Mdn3bhcKLsW0duJsKKVEFViQpqGJhvjEVZW0n2HXG+axvASArt6ADn5tZf/T4MQs" +
                            "i0C0Pk5RDjcbuRYxqWJF" +
                            "-----END CERTIFICATE-----";
                    }
                    else
                    {
                        cert =
                            "-----BEGIN CERTIFICATE-----" +
                            "MIICvjCCAaYCAgUWMA0GCSqGSIb3DQEBCwUAMGQxCzAJBgNVBAYTAlVTMQswCQYD" +
                            "VQQIDAJDQTEXMBUGA1UEBwwOUGFsbyBBbHRvIFJvb3QxEjAQBgNVBAoMCUZha2Ug" +
                            "Um9vdDEbMBkGA1UEAwwSRmFrZSBSb290IFJTQSAyMDQ4MB4XDTIxMDQxNjE3NDIx" +
                            "MFoXDTMxMDQwNDE3NDIxMFowaTELMAkGA1UEBhMCVVMxCzAJBgNVBAgMAkNBMRIw" +
                            "EAYDVQQHDAlQYWxvIEFsdG8xGTAXBgNVBAoMEEZha2UgQXR0ZXN0YXRpb24xHjAc" +
                            "BgNVBAMMFUZha2UgQXR0ZXN0YXRpb24gMTAyNDCBnzANBgkqhkiG9w0BAQEFAAOB" +
                            "jQAwgYkCgYEAsb9rznDLfq5/hGbCkj+VJSLhty54Ylih2lRL30CcF4jocUQraMF/" +
                            "SeIo9eGQec78K5qMXzJ9rruGQfEL7s7XTYkimSnp6sebqleJIvYJ7Na3pnLsBqPw" +
                            "thrw2PwaXeKQ4FswEb3N0DzFVSonNiQkW+f6UlJL1EcBrTgvudTTyK8CAwEAATAN" +
                            "BgkqhkiG9w0BAQsFAAOCAQEAX/rUsC0iFk4jn94/KNAuvRNDRe9zdwM9zmzaGjBh" +
                            "d/mCi85F+VhxwMkQa32IaMwIH2nHy2Sdt8+O3EulXjOkbSclilmmws7yR38fFIV2" +
                            "S1/zbYgrevd+DYyZ6VDb4iFcScJGQ/W257NagK9JCuC1k7cWwcRfOSxnbnv8SzAa" +
                            "WRzzLeMj7wvJDx64yccj7a3Ap89AZ3VHVaUyTD5N5IqkksElHxx7KrdqjvtvzBpw" +
                            "gHyn4hFRvSuWOio6lklE/1hgDtJ+FbR1aGMZBz4D5YseKjYQ8dYhJE5L5bgklhUi" +
                            "BRl88HhQ1aHTDbx3b8ahshSdbhG6JLmGfZBdp18XPljnwQ==" +
                            "-----END CERTIFICATE-----";
                    }

                    privateKey = KeyRsaPrivate1024;

                    break;

                case PivAlgorithm.Rsa2048:
                    if (validAttest)
                    {
                        cert =
                            "-----BEGIN CERTIFICATE-----" +
                            "MIIDXzCCAkegAwIBAgICBRkwDQYJKoZIhvcNAQELBQAwZDELMAkGA1UEBhMCVVMx" +
                            "CzAJBgNVBAgMAkNBMRcwFQYDVQQHDA5QYWxvIEFsdG8gUm9vdDESMBAGA1UECgwJ" +
                            "RmFrZSBSb290MRswGQYDVQQDDBJGYWtlIFJvb3QgUlNBIDIwNDgwHhcNMjEwNDE2" +
                            "MTc0MjE1WhcNMzEwNDA0MTc0MjE1WjBpMQswCQYDVQQGEwJVUzELMAkGA1UECAwC" +
                            "Q0ExEjAQBgNVBAcMCVBhbG8gQWx0bzEZMBcGA1UECgwQRmFrZSBBdHRlc3RhdGlv" +
                            "bjEeMBwGA1UEAwwVRmFrZSBBdHRlc3RhdGlvbiAyMDQ4MIIBIjANBgkqhkiG9w0B" +
                            "AQEFAAOCAQ8AMIIBCgKCAQEA239gOKe2rLurJ/QH5OAfJnGdQJCamKeVQX+gNsUV" +
                            "xLA8Q/6gX/HE4XmhOgeqC9M0s/S34Lm/1/wPnvDLHDGyAw5vvWmsRXaNgFEKj2eR" +
                            "dPK8Uayw05MoocGHFHIheCjvtuAeRnuVd7Ok6SpcrvCAXgx9DY2e3bEP5KyVZRnc" +
                            "BUh1pt2tQIuMiTM3ofCKuYC+tON77Q/QNm0MzOJFFsPUIcCf5jIvS0f2C8U91AAg" +
                            "bylKq1RWOXcYnHEbD/e3kevD+e6KLHka46a0Nxpf9SC8PVvlTqqV7LqgNJcxXtGo" +
                            "OeiFLnx9evLmtngsx8nndLLoG7iMeslg+XbkuE8z/6wkgQIDAQABoxYwFDASBgNV" +
                            "HRMBAf8ECDAGAQH/AgEBMA0GCSqGSIb3DQEBCwUAA4IBAQBkJB6CYu/+2NQbnQ69" +
                            "B2XXaR6AXxyL8XVB/d91Ei4ZViloFUZY4jpJ7yAEN+U6R824V/WWDMGhoIgm5u1L" +
                            "qTfi+Uqc6lTHNxWEP4B7nH1VOgh8ego9anenkTWtr6m+RTwJF3TpfIuZTJPkdU3Z" +
                            "eFyv3OQ9TWLxwGQOqT1Mx8km/xM18PawCcrYXX3AHddYUBtdPEAVfDakWn6coL1+" +
                            "oXKVHI79VrTGOKx1W0ZQefwlz7OaJn3JaBlJzUys/dXwpqnbRogiZoHLSK3uEMga" +
                            "JkKe+c+ul+dWtd3ykTWaciknQILyFwNu+MbEYOATl4BGR+/gOrw5hmAsfdV3bde0" +
                            "R9WM" +
                            "-----END CERTIFICATE-----";
                    }
                    else
                    {
                        cert =
                            "-----BEGIN CERTIFICATE-----" +
                            "MIIDQjCCAioCAgUYMA0GCSqGSIb3DQEBCwUAMGQxCzAJBgNVBAYTAlVTMQswCQYD" +
                            "VQQIDAJDQTEXMBUGA1UEBwwOUGFsbyBBbHRvIFJvb3QxEjAQBgNVBAoMCUZha2Ug" +
                            "Um9vdDEbMBkGA1UEAwwSRmFrZSBSb290IFJTQSAyMDQ4MB4XDTIxMDQxNjE3NDIx" +
                            "NVoXDTMxMDQwNDE3NDIxNVowaTELMAkGA1UEBhMCVVMxCzAJBgNVBAgMAkNBMRIw" +
                            "EAYDVQQHDAlQYWxvIEFsdG8xGTAXBgNVBAoMEEZha2UgQXR0ZXN0YXRpb24xHjAc" +
                            "BgNVBAMMFUZha2UgQXR0ZXN0YXRpb24gMjA0ODCCASIwDQYJKoZIhvcNAQEBBQAD" +
                            "ggEPADCCAQoCggEBANt/YDintqy7qyf0B+TgHyZxnUCQmpinlUF/oDbFFcSwPEP+" +
                            "oF/xxOF5oToHqgvTNLP0t+C5v9f8D57wyxwxsgMOb71prEV2jYBRCo9nkXTyvFGs" +
                            "sNOTKKHBhxRyIXgo77bgHkZ7lXezpOkqXK7wgF4MfQ2Nnt2xD+SslWUZ3AVIdabd" +
                            "rUCLjIkzN6HwirmAvrTje+0P0DZtDMziRRbD1CHAn+YyL0tH9gvFPdQAIG8pSqtU" +
                            "Vjl3GJxxGw/3t5Hrw/nuiix5GuOmtDcaX/UgvD1b5U6qley6oDSXMV7RqDnohS58" +
                            "fXry5rZ4LMfJ53Sy6Bu4jHrJYPl25LhPM/+sJIECAwEAATANBgkqhkiG9w0BAQsF" +
                            "AAOCAQEAZZwTv0VapJd4Wbcdr3fI/sarBVw6NTrqcK85LdkGF8Nyh+RwNjwYoCjM" +
                            "0x7PQ4w6CDzHsoy5LgHVh2i6EDrwKWTuJEQjxzKmzbGy5CVC6WenlQRs54GSkAiK" +
                            "t6S38Z93mlotFD3bQ6aRC70yRZhm392dD3TJoJ04I6ut9h1C3/AVPEAD+S7DBa+j" +
                            "HOLCfWP3fsFcf0vtKLbGnwz/OJdVVk8qW2SRbue7fYEqnC/10oYin2pAi7QMunFJ" +
                            "l8YdWAD7z59wslyIzXi5WivlB592+P78xFNl/QS00nKzO9eXv3/HM006Vb9BLmK0" +
                            "WvIT54DcVb+MwcRQvCyzgcWxekMtPg==" +
                            "-----END CERTIFICATE-----";
                    }

                    privateKey = KeyRsaPrivate2048;
                    
                    break;
                case PivAlgorithm.Rsa3072:
                    cert = CertRsa3072;
                    privateKey = KeyRsaPrivate3072;
                    
                    break;
                case PivAlgorithm.Rsa4096:
                    cert = CertRsa4096;
                    privateKey = KeyRsaPrivate4096;
                    
                    break;
                case PivAlgorithm.EccP256:
                    if (validAttest)
                    {
                        cert =
                            "-----BEGIN CERTIFICATE-----" +
                            "MIICkzCCAXugAwIBAgICBRMwDQYJKoZIhvcNAQELBQAwZDELMAkGA1UEBhMCVVMx" +
                            "CzAJBgNVBAgMAkNBMRcwFQYDVQQHDA5QYWxvIEFsdG8gUm9vdDESMBAGA1UECgwJ" +
                            "RmFrZSBSb290MRswGQYDVQQDDBJGYWtlIFJvb3QgUlNBIDIwNDgwHhcNMjEwNDE2" +
                            "MTc0MTQ0WhcNMzEwNDA0MTc0MTQ0WjBoMQswCQYDVQQGEwJVUzELMAkGA1UECAwC" +
                            "Q0ExEjAQBgNVBAcMCVBhbG8gQWx0bzEZMBcGA1UECgwQRmFrZSBBdHRlc3RhdGlv" +
                            "bjEdMBsGA1UEAwwURmFrZSBBdHRlc3RhdGlvbiAyNTYwWTATBgcqhkjOPQIBBggq" +
                            "hkjOPQMBBwNCAASATc5kxXt5D5v6oVIN1DlMJD2mCDb13GZQZfzujCFsEeBT5GuU" +
                            "13leL+p9yOXSoPXIm4zS+Dmbg2OpPfPHqrT5oxYwFDASBgNVHRMBAf8ECDAGAQH/" +
                            "AgEBMA0GCSqGSIb3DQEBCwUAA4IBAQBn/qQj/dQpK1lddIwV1BI8kOoZ15qyZwmd" +
                            "KczLLApYzRFzTdfOYXpxxG8ze+b8VuPPml10urFO22LQSL2FAMLqRn5sbsoIVWqu" +
                            "/A6TleqKj0OB8jLCEJd/KJB0HnVLQPY6LnSEvpkUXAzar+ltujyTTZl8K4gahbjs" +
                            "ghZG7cD2QnY0Awy95dpIn+6jNM9GU/Fvc5yuRbdkuF61X/GrV03YMokkpeW0NhCa" +
                            "UP/OXsi9JexE/mLV46HptVs/g8yoo7fyT8oXkoASlKf0pWVEsSzo2EL+HGb5Twfh" +
                            "ixWFfZ+FAdr3jWQa4aq1HA/lwQppVqSIJ0+jnkkbgTwRw8ve21BP" +
                            "-----END CERTIFICATE-----";
                    }
                    else
                    {
                        cert =
                            "-----BEGIN CERTIFICATE-----" +
                            "MIICdjCCAV4CAgUSMA0GCSqGSIb3DQEBCwUAMGQxCzAJBgNVBAYTAlVTMQswCQYD" +
                            "VQQIDAJDQTEXMBUGA1UEBwwOUGFsbyBBbHRvIFJvb3QxEjAQBgNVBAoMCUZha2Ug" +
                            "Um9vdDEbMBkGA1UEAwwSRmFrZSBSb290IFJTQSAyMDQ4MB4XDTIxMDQxNjE3NDE0" +
                            "NFoXDTMxMDQwNDE3NDE0NFowaDELMAkGA1UEBhMCVVMxCzAJBgNVBAgMAkNBMRIw" +
                            "EAYDVQQHDAlQYWxvIEFsdG8xGTAXBgNVBAoMEEZha2UgQXR0ZXN0YXRpb24xHTAb" +
                            "BgNVBAMMFEZha2UgQXR0ZXN0YXRpb24gMjU2MFkwEwYHKoZIzj0CAQYIKoZIzj0D" +
                            "AQcDQgAEgE3OZMV7eQ+b+qFSDdQ5TCQ9pgg29dxmUGX87owhbBHgU+RrlNd5Xi/q" +
                            "fcjl0qD1yJuM0vg5m4NjqT3zx6q0+TANBgkqhkiG9w0BAQsFAAOCAQEAlzLV+ZSB" +
                            "ORU3s/qKA/uzC414Ilpe66BXAvc+trgJZk2FmUrhnvzr/JTCcgL5knjBlRsM2/CW" +
                            "oNAVqNvyQTzv5nHEVrNhWzTZNzvO54NWP7EMJ7d0tZVn0brEycnPu8MLGshS7Hgz" +
                            "gfWtLptAjJx6d7aJlDp5EupNZre51fViRuVKwB9f2dgvm9q2jtoMZ9+YdnCwzB/5" +
                            "NXjE/O7CTw53kelxKEY4AbWdMOhMG+WQtbYbe7Pk1KM9EWdiHNg7dX1jViD8ysVP" +
                            "9kblGWuh+OCNzurVvHtZQswIseNareOwVk13Mqk7Pq9zVrMi9Qn+rpjlSiTfop/W" +
                            "+eK+8+M1p6TEZA==" +
                            "-----END CERTIFICATE-----";
                    }

                    privateKey = KeyEccPrivateP256;

                    break;

                case PivAlgorithm.EccP384:
                    if (validAttest)
                    {
                        cert =
                            "-----BEGIN CERTIFICATE-----" +
                            "MIICsDCCAZigAwIBAgICBRUwDQYJKoZIhvcNAQELBQAwZDELMAkGA1UEBhMCVVMx" +
                            "CzAJBgNVBAgMAkNBMRcwFQYDVQQHDA5QYWxvIEFsdG8gUm9vdDESMBAGA1UECgwJ" +
                            "RmFrZSBSb290MRswGQYDVQQDDBJGYWtlIFJvb3QgUlNBIDIwNDgwHhcNMjEwNDE2" +
                            "MTc0MjA1WhcNMzEwNDA0MTc0MjA1WjBoMQswCQYDVQQGEwJVUzELMAkGA1UECAwC" +
                            "Q0ExEjAQBgNVBAcMCVBhbG8gQWx0bzEZMBcGA1UECgwQRmFrZSBBdHRlc3RhdGlv" +
                            "bjEdMBsGA1UEAwwURmFrZSBBdHRlc3RhdGlvbiAzODQwdjAQBgcqhkjOPQIBBgUr" +
                            "gQQAIgNiAAQMJtrJS7oUVxb9ofXTeGWHRzDyz+DEzktNNP32w1lk4W1xJYR7R0Uj" +
                            "uhDiRkc7wC4e3UWN+wHUGLtodeuMLnnxvp40psR3k/SVUbCn6UP0QFF/JOTv9fGt" +
                            "qfccBGVNHt+jFjAUMBIGA1UdEwEB/wQIMAYBAf8CAQEwDQYJKoZIhvcNAQELBQAD" +
                            "ggEBAKoM0ZlWkh11NtpzL46F/JOYzBbptS+CJiEC4SAZwYDEZrW7zkGko8rBVO8q" +
                            "HpzRcNP88hW7YKHsrmTX3U3zJJZ96VxHT0R6zXMsZeOmkGT4tvjarGU2KJKKmN0Q" +
                            "aRdIqiUApTcvBVICXJPJeAmIClQZ1AdMWf0sijikh5eiq44PkuJNj6gCu0UzZguB" +
                            "Tio6GosI4lH58YviZi0WfyM19MS9MWLg3SGJniUwwI57+15Z5979IcUlC37UXLCY" +
                            "oBn8zsluxvYqdKlFbUhy1x6C2UT2YWOzkqpBHtcC1uNG/AnnnL695WASdIw+qmd4" +
                            "I5e05u1HmEVWQGbtX+DXtkrEGgw=" +
                            "-----END CERTIFICATE-----";
                    }
                    else
                    {
                        cert =
                            "-----BEGIN CERTIFICATE-----" +
                            "MIICkzCCAXsCAgUUMA0GCSqGSIb3DQEBCwUAMGQxCzAJBgNVBAYTAlVTMQswCQYD" +
                            "VQQIDAJDQTEXMBUGA1UEBwwOUGFsbyBBbHRvIFJvb3QxEjAQBgNVBAoMCUZha2Ug" +
                            "Um9vdDEbMBkGA1UEAwwSRmFrZSBSb290IFJTQSAyMDQ4MB4XDTIxMDQxNjE3NDIw" +
                            "NVoXDTMxMDQwNDE3NDIwNVowaDELMAkGA1UEBhMCVVMxCzAJBgNVBAgMAkNBMRIw" +
                            "EAYDVQQHDAlQYWxvIEFsdG8xGTAXBgNVBAoMEEZha2UgQXR0ZXN0YXRpb24xHTAb" +
                            "BgNVBAMMFEZha2UgQXR0ZXN0YXRpb24gMzg0MHYwEAYHKoZIzj0CAQYFK4EEACID" +
                            "YgAEDCbayUu6FFcW/aH103hlh0cw8s/gxM5LTTT99sNZZOFtcSWEe0dFI7oQ4kZH" +
                            "O8AuHt1FjfsB1Bi7aHXrjC558b6eNKbEd5P0lVGwp+lD9EBRfyTk7/Xxran3HARl" +
                            "TR7fMA0GCSqGSIb3DQEBCwUAA4IBAQAvioQty65EJejEJjxY4u4poMsEKC++HTzF" +
                            "RcLB0zkWxcO4oxzDW11gogjAslA4QSfop79P33ln4uZ3aDHczEhguFcnJQ9Takwn" +
                            "FQsXHOHCL2HupDyaQMznjPZrJYcv9jTUtSJ7IVQP8xYnN2eKi9vB5FeKL1UphM/B" +
                            "FMUqrIsZIcL+sCi0Be1skAj82/+C+ny9GOEriMRkMN/WoAscuNIIP/E2JX1kCJbw" +
                            "uJMBWPe8kGuzUsJ+iblLvTOd2dwDu5EtTJcESW+2zzwwSW1O41aS36ARrct/A3rQ" +
                            "e510vuxfCvR7kt74bSuKi3wxsCTLtMEfIh51k3xZsa4FoLO8mm4v" +
                            "-----END CERTIFICATE-----";
                    }

                    privateKey = KeyEccPrivateP384;

                    break;
            }

            return true;
        }

        public static PivPublicKey GetPivPublicKey(PivAlgorithm algorithm) => ConvertPemKeyString(GetPemKeyString(algorithm)).GetPivPublicKey();

        // Get a private key for the given algorithm. Return the key as a PivPrivateKey.
        public static PivPrivateKey GetPivPrivateKey(PivAlgorithm algorithm) => ConvertPemKeyString(GetPemKeyString(algorithm)).GetPivPrivateKey();

        public static X509Certificate2 GetCert(PivAlgorithm algorithm) => ConvertPemCertString(GetPemCertString(algorithm)).GetCertObject();
        
        public static KeyConverter ConvertPemKeyString(string pemString) =>
            new KeyConverter(pemString.ToCharArray());
        
        public static CertConverter ConvertPemCertString(string pemString) =>
            new CertConverter(pemString.ToCharArray());
        private static string GetPemCertString(PivAlgorithm algorithm) =>
            algorithm switch
            {
                PivAlgorithm.Rsa1024 => CertRsa1024,
                PivAlgorithm.Rsa2048 => CertRsa2048,
                PivAlgorithm.Rsa3072 => CertRsa3072,
                PivAlgorithm.Rsa4096 => CertRsa4096,
                _ => throw new ArgumentException("No cert mapped", nameof(algorithm))
            };
        
        private static string GetPemKeyString(PivAlgorithm algorithm) =>
            algorithm switch
            {
                PivAlgorithm.Rsa1024 => KeyRsaPrivate1024,
                PivAlgorithm.Rsa2048 => KeyRsaPrivate2048,
                PivAlgorithm.Rsa3072 => KeyRsaPrivate3072,
                PivAlgorithm.Rsa4096 => KeyRsaPrivate4096,
                PivAlgorithm.EccP256 => KeyEccPrivateP256,
                PivAlgorithm.EccP384 => KeyEccPrivateP384,
                _ => throw new ArgumentException("No key mapped", nameof(algorithm))
            };
    }
}
