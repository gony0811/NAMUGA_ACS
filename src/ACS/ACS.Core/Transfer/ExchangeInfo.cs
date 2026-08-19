using System;
using System.Collections.Generic;
using System.Text;

namespace ACS.Core.Transfer
{
    /// <summary>
    /// EXCHANGE TransportCommand 의 AdditionalInfo 키-값 규약("KEY=VALUE;KEY=VALUE;...")의
    /// 단일 파서/빌더. AdditionalInfo 문자열 조작은 반드시 이 클래스를 통해서만 수행한다.
    ///
    /// 규약 (ACS_EXCHANGE_구현사양서.md §2.2):
    ///  - 항목 구분자는 ';', 키-값 구분자는 첫 번째 '='. (값 안의 '=' 허용, ';' 금지)
    ///  - 키는 대문자 권장. 조회는 대소문자 무시(OrdinalIgnoreCase).
    ///  - 항목 순서는 입력 순서를 보존한다.
    /// </summary>
    public static class ExchangeInfo
    {
        /// <summary>현재 EXCHANGE 단계 (10/20/30/40/50/60). crash 복구의 근거.</summary>
        public const string KEY_STEP = "STEP";
        /// <summary>배칭 트립 ID. 같은 트립의 TC 들은 동일 값.</summary>
        public const string KEY_TRIP = "TRIP";
        /// <summary>신규 매거진 투입 슬롯 번호 (1|2).</summary>
        public const string KEY_LOADSLOT = "LOADSLOT";
        /// <summary>기존 매거진 회수 슬롯 번호 (3|4).</summary>
        public const string KEY_UNLOADSLOT = "UNLOADSLOT";
        /// <summary>설비 LOAD 보고용 JobID (EXCHANGECMD.LoadEquipJobID).</summary>
        public const string KEY_EQJOB_L = "EQJOB_L";
        /// <summary>설비 UNLOAD 보고용 JobID (EXCHANGECMD.UnloadEquipJobID).</summary>
        public const string KEY_EQJOB_U = "EQJOB_U";
        /// <summary>
        /// 진행 중인 설비 액션 (UNLOAD | LOAD | 빈값).
        /// MES ACTIONCMD 를 차량으로 중계할 때 기록하고, 해당 액션의 완료 reply 를
        /// 처리할 때 클리어한다. 빈값이면 설비 액션 미진행 — 이때 도착한
        /// EXCHANGE COMPLETED reply 는 이동/도킹 완료로 간주하고 무시한다.
        /// </summary>
        public const string KEY_ACT = "ACT";

        /// <summary>KEY_ACT 값: 기존 매거진 회수 액션 (사양서 ACTIONCMD Type=UNLOAD).</summary>
        public const string ACT_UNLOAD = "UNLOAD";
        /// <summary>KEY_ACT 값: 신규 매거진 투입 액션 (사양서 ACTIONCMD Type=LOAD).</summary>
        public const string ACT_LOAD = "LOAD";

        /// <summary>
        /// 도착(ARRIVED) 보고 idempotency 마커. 도착 보고를 발행한 뒤 기록하고, 같은 값이면 재보고를 생략한다.
        /// pose 기반 도착 판정과 AMR reply(ARRIVED) 가 같은 도착에 대해 이중 트리거되는 것을 막는다.
        /// 값: EXCHANGE TC = 보고한 step(예 "20"), 일반 TC = "&lt;nodeId&gt;|&lt;tcState&gt;".
        /// </summary>
        public const string KEY_ARRIVED = "ARRIVED";

        private const char ENTRY_SEPARATOR = ';';
        private const char KEYVALUE_SEPARATOR = '=';

        /// <summary>
        /// AdditionalInfo 문자열을 순서 보존 키-값 목록으로 파싱한다.
        /// null/공백이면 빈 목록. '=' 없는 항목은 값 "" 로 취급.
        /// </summary>
        public static List<KeyValuePair<string, string>> Parse(string additionalInfo)
        {
            var result = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrWhiteSpace(additionalInfo))
                return result;

            string[] entries = additionalInfo.Split(ENTRY_SEPARATOR);
            foreach (string entry in entries)
            {
                string trimmed = entry.Trim();
                if (trimmed.Length == 0)
                    continue;

                int sep = trimmed.IndexOf(KEYVALUE_SEPARATOR);
                if (sep < 0)
                {
                    result.Add(new KeyValuePair<string, string>(trimmed, ""));
                }
                else
                {
                    string key = trimmed.Substring(0, sep).Trim();
                    string value = trimmed.Substring(sep + 1).Trim();
                    if (key.Length == 0)
                        continue; // "=value" 형태의 비정상 항목은 무시
                    result.Add(new KeyValuePair<string, string>(key, value));
                }
            }
            return result;
        }

        /// <summary>
        /// 키-값 목록을 규약 문자열로 조립한다. 값에 ';' 가 있으면 ArgumentException.
        /// </summary>
        public static string Build(IEnumerable<KeyValuePair<string, string>> entries)
        {
            if (entries == null)
                return "";

            var sb = new StringBuilder();
            foreach (var kv in entries)
            {
                ValidateKey(kv.Key);
                ValidateValue(kv.Key, kv.Value);
                if (sb.Length > 0)
                    sb.Append(ENTRY_SEPARATOR);
                sb.Append(kv.Key).Append(KEYVALUE_SEPARATOR).Append(kv.Value ?? "");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 키에 해당하는 값을 반환한다. 키가 없거나 입력이 null/공백이면 "" (예외 없음).
        /// 키 비교는 대소문자 무시.
        /// </summary>
        public static string Get(string additionalInfo, string key)
        {
            if (string.IsNullOrEmpty(key))
                return "";

            foreach (var kv in Parse(additionalInfo))
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value ?? "";
            }
            return "";
        }

        /// <summary>키 존재 여부 (대소문자 무시).</summary>
        public static bool Has(string additionalInfo, string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            foreach (var kv in Parse(additionalInfo))
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 키를 갱신(기존 위치 유지)하거나 없으면 끝에 추가한 새 문자열을 반환한다.
        /// value 가 null 이면 "" 로 저장. 원본 문자열은 변경하지 않는다.
        /// </summary>
        public static string Set(string additionalInfo, string key, string value)
        {
            ValidateKey(key);
            ValidateValue(key, value);

            var entries = Parse(additionalInfo);
            bool replaced = false;
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    entries[i] = new KeyValuePair<string, string>(entries[i].Key, value ?? "");
                    replaced = true;
                    break;
                }
            }
            if (!replaced)
                entries.Add(new KeyValuePair<string, string>(key, value ?? ""));

            return Build(entries);
        }

        /// <summary>
        /// EXCHANGE TC 최초 insert 시의 AdditionalInfo 를 조립한다 (구현사양서 §2.1).
        /// STEP=10, TRIP/LOADSLOT/UNLOADSLOT/ACT 는 빈 값으로 예약.
        /// </summary>
        public static string BuildInitial(string equipLoadJobId, string equipUnloadJobId)
        {
            return Build(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(KEY_STEP, "10"),
                new KeyValuePair<string, string>(KEY_TRIP, ""),
                new KeyValuePair<string, string>(KEY_LOADSLOT, ""),
                new KeyValuePair<string, string>(KEY_UNLOADSLOT, ""),
                new KeyValuePair<string, string>(KEY_EQJOB_L, equipLoadJobId ?? ""),
                new KeyValuePair<string, string>(KEY_EQJOB_U, equipUnloadJobId ?? ""),
                new KeyValuePair<string, string>(KEY_ACT, "")
            });
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("ExchangeInfo: key must not be null or empty");
            if (key.IndexOf(ENTRY_SEPARATOR) >= 0 || key.IndexOf(KEYVALUE_SEPARATOR) >= 0)
                throw new ArgumentException($"ExchangeInfo: key must not contain '{ENTRY_SEPARATOR}' or '{KEYVALUE_SEPARATOR}' - key='{key}'");
        }

        private static void ValidateValue(string key, string value)
        {
            if (value != null && value.IndexOf(ENTRY_SEPARATOR) >= 0)
                throw new ArgumentException($"ExchangeInfo: value must not contain '{ENTRY_SEPARATOR}' - key='{key}', value='{value}'");
        }
    }
}
