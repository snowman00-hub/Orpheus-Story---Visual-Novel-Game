using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using UnityEngine;

// Resources 폴더에 있는 대사 CSV를 읽어 id 기반 대사 목록을 만든다.
public static class DialogueCsvLoader
{
    private static readonly string[] ChapterResourcePaths =
    {
        "Dialogues/ch01",
        "Dialogues/ch02",
        "Dialogues/ch03",
        "Dialogues/ch04",
        "Dialogues/ch05",
        "Dialogues/ch06",
        "Dialogues/ch07"
    };

    // 기본 챕터 CSV들을 읽어 하나의 대사 사전으로 합친다.
    public static Dictionary<string, DialogueLine> LoadAllChaptersFromResources()
    {
        return LoadFromResources(ChapterResourcePaths);
    }

    // 여러 CSV를 Resources 경로로 읽어 하나의 대사 사전으로 합친다.
    public static Dictionary<string, DialogueLine> LoadFromResources(IEnumerable<string> resourcePaths)
    {
        var linesById = new Dictionary<string, DialogueLine>();

        foreach (string resourcePath in resourcePaths)
        {
            TextAsset csvAsset = Resources.Load<TextAsset>(resourcePath);
            if (csvAsset == null)
            {
                Debug.LogWarning($"Dialogue CSV not found in Resources: {resourcePath}");
                continue;
            }

            foreach (DialogueLine line in Parse(csvAsset.text))
            {
                if (linesById.ContainsKey(line.Id))
                {
                    Debug.LogWarning($"Duplicate dialogue id skipped: {line.Id}");
                    continue;
                }

                linesById.Add(line.Id, line);
            }
        }

        return linesById;
    }

    // CSV 텍스트 하나를 DialogueLine 목록으로 파싱한다.
    private static IEnumerable<DialogueLine> Parse(string csvText)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant()  // 대소문자 구분 없이 매칭하도록 설정
        };

        using var reader = new StringReader(csvText);
        using var csv = new CsvReader(reader, config);

        foreach (DialogueLine line in csv.GetRecords<DialogueLine>())
        {
            yield return line;
        }
    }
}
