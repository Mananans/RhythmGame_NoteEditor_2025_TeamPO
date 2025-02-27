using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Threading;
using System;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

public class DefaultWindow : MonoBehaviour
{
    public AudioSourceSeparation ass;

    [Space(10)]
    [Header("UI")]
    [SerializeField] Dropdown dropdown;
    [SerializeField] Button refreshButton;
    [SerializeField] Button LoadSongButton;
    [SerializeField] InputField LineCount;
    [SerializeField] Button SaveUJSONData;
    [SerializeField] AudioSource audioPlayer;
    [SerializeField] Font rslFont;
    [SerializeField] GameObject LoadSongMask;
    [SerializeField] InputField tagString;

    [Space(10)]
    [Header("Data")]
    int maxiNodeLength;
    [SerializeField] List<string> songDDList;
    [SerializeField] string seletedSongName;
    [SerializeField] List<NoteList> noteList;
    [SerializeField] VisibilityManager vm;
    string signature = string.Empty;
    int measureDuration;
    int lengthMeasure;

    [Space(10)]
    [SerializeField] Transform createNotesRoot;
    [SerializeField] List<w_NoteList> writeNoteList;
    
    [Space(10)]
    [SerializeField] U_JSONData u_jsonData;
    Thread th;

    [Space(10)]
    Color baseColor = Color.red;
    Color drumColor = Color.green;
    Color pianoColor = Color.blue;
    Color vocalsColor = Color.cyan;
    Color otherColor = Color.yellow;
    Color nullColor = new Color(84 / 255, 84 / 255, 84 / 255);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadMP3List();
        u_jsonData = new U_JSONData();
        u_jsonData.notes = new Dictionary<string, List<U_Note>>();
        u_jsonData.gameStats = new GameState();
        u_jsonData.songInfo = new SongInfo();
        th = new Thread(new ThreadStart(ass.Activate));
        refreshButton.onClick.AddListener(LoadMP3List);
        LoadSongButton.onClick.AddListener(delegate { 
            ass.SongName = seletedSongName;
            LoadSongMask.SetActive(true);
            //ass.Activate();
            th.Start();
            StartCoroutine(waitSetNoteGuid());
        });
        SaveUJSONData.onClick.AddListener(SaveUJsonButtonEvent);
        dropdown.onValueChanged.AddListener(delegate{ seletedSongName = songDDList[dropdown.value].Split('.')[0]; });
    }

    private void OnApplicationQuit()
    {
        if (th != null)
        {
            th.Abort();
            th = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void LoadMP3List()
    {
        dropdown.ClearOptions();

        DirectoryInfo di = new DirectoryInfo(Application.streamingAssetsPath + @"/Songs");
        FileInfo[] fi = di.GetFiles("*.mp3");
        songDDList = new List<string>();
        foreach (FileInfo mp3File in fi)
        {
            songDDList.Add(mp3File.Name);
        }

        dropdown.AddOptions(songDDList);
        seletedSongName = songDDList[0].Split('.')[0];
    }


    //note 관련 데이터를 불러오고, 음악 데이터를 불러오는 등 외부 파일을 읽어들여 기본 세팅을 함.
    IEnumerator waitSetNoteGuid()
    {
        while(!ass.EndWorkTrigger)
            yield return new WaitForEndOfFrame();

        ass.EndWorkTrigger = false;

        yield return new WaitForSeconds(0.5f);
        string path = Application.streamingAssetsPath + $"/spleeter_output/{seletedSongName}/mml/";
        string bassData = File.ReadAllText(path + "bass.mml");
        string pianoData = File.ReadAllText(path + "piano.mml");
        string vocalsData = File.ReadAllText(path + "vocals.mml");
        string drumsData = File.ReadAllText(path + "drums.mml");
        string otherData = File.ReadAllText(path + "other.mml");

        NoteList bassNoteList;
        NoteList pianoNoteList;
        NoteList vocalsNoteList;
        NoteList drumsNoteList;
        NoteList otherNoteList;

        bassNoteList = JsonUtility.FromJson<NoteList>("{\"notes\":" + bassData + "}");
        pianoNoteList = JsonUtility.FromJson<NoteList>("{\"notes\":" + pianoData + "}");
        vocalsNoteList = JsonUtility.FromJson<NoteList>("{\"notes\":" + vocalsData + "}");
        drumsNoteList = JsonUtility.FromJson<NoteList>("{\"notes\":" + drumsData + "}");
        otherNoteList = JsonUtility.FromJson<NoteList>("{\"notes\":" + otherData + "}");

        Debug.Log("END NODE DATA");

        noteList.Add(bassNoteList);
        noteList.Add(pianoNoteList);
        noteList.Add(vocalsNoteList);
        noteList.Add(drumsNoteList);
        noteList.Add(otherNoteList);

        noteList[0].instrument = "base";
        noteList[1].instrument = "piano";
        noteList[2].instrument = "vocals";
        noteList[3].instrument = "drums";
        noteList[4].instrument = "other";

        int LineLength;
        if (int.TryParse(LineCount.text, out LineLength))
        {
            int distanceLineValue = LineLength - noteList.Count;

            if (distanceLineValue > 0)
                SplitNoteLists(LineLength);
            else
            {
                for (int i = 0; i < Mathf.Abs(distanceLineValue); i++)
                {
                    int removeTargetIndex = UnityEngine.Random.Range(0, noteList.Count);
                    MergeNoteList(removeTargetIndex);
                }
            }
        }

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(Application.streamingAssetsPath + $"/Songs/{seletedSongName}.mp3", AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            audioPlayer.clip = clip;
        }

        signature = string.Empty;
        StreamReader sr = new StreamReader(Application.streamingAssetsPath + "/tempo_output.txt");
        string data = sr.ReadLine();
        data = data.Split(' ')[1];
        data = data.Replace('[', ' ');
        data = data.Replace(']', ' ');
        data = string.Concat(data.Where(x => !char.IsWhiteSpace(x)));
        u_jsonData.songInfo.bpm = (int)float.Parse(data);

        data = sr.ReadLine();
        data = data.Split(' ')[1];

        u_jsonData.songInfo.duration = float.Parse(data);
        sr.ReadLine();
        sr.ReadLine();
        signature = sr.ReadLine().Split(' ')[2];
        sr.Close();

        measureDuration = (int) (GetMeasureDuration(signature, u_jsonData.songInfo.bpm)* 10);

        Convert_w_Note();
        fixeNoteList();
        CreateGuidNodes();
        CreateNodes();

        LoadSongMask.SetActive(false);
        noteList = null;
    }
    //불러온 Node Data를 편집용 Node 데이터로 치환.
    void Convert_w_Note()
    {
        writeNoteList = new List<w_NoteList>();
        double bpm = u_jsonData.songInfo.bpm;  // BPM 값
        double songDuration = u_jsonData.songInfo.duration;  // 노래 길이 (초)

        for (int i = 0; i < noteList.Count; i++)
        {
            writeNoteList.Add(new w_NoteList());
            writeNoteList[i].notes = new List<w_NoteData>();
            int xCount = 0;
            for (int j = 0; j < noteList[i].notes.Count; j++)
            {

                string length = noteList[i].notes[j].length;
                string instrument = noteList[i].notes[j].instrument;

                switch (length)
                {
                    case "l16":
                        SetW_Node(i, noteList[i].notes[j].value, noteList[i].notes[j].instrument);
                        xCount++;
                        break;
                    case "l8":
                        for (int z = 0; z < 2; z++)
                        {
                            SetW_Node(i, noteList[i].notes[j].value, noteList[i].notes[j].instrument);
                            xCount++;
                        }
                        break;
                    case "l4":
                        for (int z = 0; z < 4; z++)
                        {
                            SetW_Node(i, noteList[i].notes[j].value, noteList[i].notes[j].instrument);
                            xCount++;
                        }
                        break;
                    case "l2":
                        for (int z = 0; z < 8; z++)
                        {
                            SetW_Node(i, noteList[i].notes[j].value, noteList[i].notes[j].instrument);
                            xCount++;
                        }
                        break;

                    default: // l1의 경우.
                        length = length.Split(' ')[1];
                        length = length.Replace('(', ' ');
                        length = length.Replace(')', ' ');
                        length = length.Replace('s', ' ');
                        length = string.Concat(length.Where(x => !char.IsWhiteSpace(x)));
                        float f_len = float.Parse(length);
                        int i_len = (int)(f_len * 10);
                        for (int z = 0; z < i_len; z++)
                        {
                            SetW_Node(i, noteList[i].notes[j].value, noteList[i].notes[j].instrument);
                            xCount++;
                        }
                        break;
                }
            }
        }
    }

    //치환 body 함수.
    void SetW_Node(int listIndex, string value, string instrument)
    {
        w_NoteData noteData = new w_NoteData();
        noteData.value = value;
        noteData.instrument = instrument;
        noteData.group = ((writeNoteList[listIndex].notes.Count + 1) / measureDuration).ToString();

        if (int.Parse(noteData.group) > lengthMeasure)
            lengthMeasure = int.Parse(noteData.group);

        writeNoteList[listIndex].notes.Add(noteData);
    }

    //이후 편집 작업을 위하여 w_notelist를 수정함.
    void fixeNoteList()
    {
        // 4분음표(l4) 길이 계산 (초)
        double quarterNoteLength = 60.0 / u_jsonData.songInfo.bpm;

        // 16분음표(l16) 길이 계산 (초)
        double sixteenthNoteLength = quarterNoteLength / 4;

        // 최대 l16 개수
        int maxSixteenthNotes = (int)(u_jsonData.songInfo.duration / sixteenthNoteLength);

        for (int i = 0; i < writeNoteList.Count; i++)
        {
            int noteCount = writeNoteList[i].notes.Count;

            // 만약 리스트의 크기가 maxSixteenthNotes를 초과할 경우에만 조정
            if (noteCount > maxSixteenthNotes)
            {
                int deleteCount = noteCount - maxSixteenthNotes;  // 삭제할 갯수 계산
                int middlePoint = writeNoteList[i].notes.Count / 2;
                int deletePoint = middlePoint - deleteCount / 2;

                if(deletePoint < 0)
                    deletePoint = 0;

                 // 리스트의 중간 인덱스
                 for(int j = 0; j<deleteCount; j++)
                    writeNoteList[i].notes.RemoveAt(deletePoint);
            }
        }


        int[] lineToNotesLength = new int[writeNoteList.Count];
        for (int i = 0; i < lineToNotesLength.Length; i++)
            lineToNotesLength[i] = writeNoteList[i].notes.Count;

        maxiNodeLength = lineToNotesLength.Max();


        //NoteList의 Note들 길이 깔맞춤 하기. 
        //이유 : 노래는 아직 끝나지 않았으며.
        //해당하는 Line에 추가적인 node를 삽입할 수 있으니까.

        for (int i = 0; i < writeNoteList.Count; i++)
        {
            int length = maxiNodeLength - writeNoteList[i].notes.Count;
            for (int j = 0; j < length; j++)
            {
                w_NoteData notedata = new w_NoteData();
                notedata.value = "r";
                notedata.instrument = noteList[i].instrument;
                writeNoteList[i].notes.Add(notedata);
            }
        }

        //s note인지 l note인지 기본 default 세팅을 한다.
        for(int i = 0; i<writeNoteList.Count;i++)
        {
            for(int j = 0; j < writeNoteList[i].notes.Count;j++)
            {
                if (writeNoteList[i].notes[j].value == "r")
                {
                    writeNoteList[i].notes[j].type = "r";
                    continue;
                }

                string t_value = writeNoteList[i].notes[j].value;
                string t_value_plus = string.Empty;
                string t_value_minus = string.Empty;
                if(j+1 < writeNoteList[i].notes.Count)
                {
                    t_value_plus = writeNoteList[i].notes[j+1].value;
                }

                if(j-1 > 0)
                {
                    t_value_minus = writeNoteList[i].notes[j-1].value;
                }

                if(t_value_minus.Length > 0 && t_value_plus.Length > 0)
                {
                    if (t_value_plus == t_value || t_value_minus == t_value)
                    {
                        if (t_value_plus == t_value)
                            writeNoteList[i].notes[j + 1].type = "L";
                        if (t_value_minus == t_value)
                            writeNoteList[i].notes[j - 1].type = "L";

                        writeNoteList[i].notes[j].type = "L"; 
                    }
                    else
                        writeNoteList[i].notes[j].type = "S";
                }
                else if(t_value_plus.Length > 0)
                {
                    if (t_value_plus == t_value)
                    {
                        writeNoteList[i].notes[j+1].type = "L";
                        writeNoteList[i].notes[j].type = "L";
                    }
                    else
                        writeNoteList[i].notes[j].type = "S";
                }
                else if(t_value_minus.Length > 0)
                {
                    if (t_value_minus == t_value)
                    {
                        writeNoteList[i].notes[j -1].type = "L";
                        writeNoteList[i].notes[j].type = "L"; 
                    }

                    else
                        writeNoteList[i].notes[j].type = "S";
                }
            }
        }

    }

    public void CreateGuidNodes()
    {
        u_jsonData.songInfo.title = seletedSongName;

        float nBGWidth = 60 * measureDuration + 20 * measureDuration - 10;
        float nBGHeight = 800;

        int loopCount = maxiNodeLength % measureDuration == 0 ? maxiNodeLength / measureDuration : maxiNodeLength / measureDuration + 1;
        for (int j = 0; j < loopCount + 1; j++)
        {
            GameObject go = new GameObject("noteBG");
            go.transform.SetParent(createNotesRoot, false);
            RectTransform nBGrt = go.AddComponent<RectTransform>();
            nBGrt.pivot = Vector2.zero;
            nBGrt.anchorMin = Vector2.zero;
            nBGrt.anchorMax = Vector2.zero;
            nBGrt.localScale = Vector3.one;

            nBGrt.sizeDelta = new Vector2(nBGWidth, nBGHeight);
            nBGrt.anchoredPosition = new Vector3(j * nBGWidth + 10 * j, 0, 0);

            RawImage ri = go.AddComponent<RawImage>();

            ri.color = new Color(1, 0, 1, 0.5f);
            go.SetActive(false);
            vm.targetObjects.Add(go);
        }
    }

    public void CreateNodes()
    { 
        #region Node 만들기
        float width = 60;                                   //height가 line 갯수와 딱 맞으면 여백이 없으니 여백용 크기조절
        float height = (800 / noteList.Count) - (20 - noteList.Count);
        float blankSize = 20;

        float posx = width; //* + 여백 20 + width;
        float posy = height; // height + 여백 20씩 증가.
        for(int i = 0; i< writeNoteList.Count; i++) 
        {
            for (int j = 0; j < writeNoteList[i].notes.Count; j++) 
            {
                string instrument = writeNoteList[i].notes[j].instrument;
                Color c_temp = Color.white;
                int i_temp = i;
                int v_temp = j;
                if (writeNoteList[i].notes[j].value == "r")
                    c_temp = nullColor;
                else
                    switch(instrument)
                    {
                        case "bass":
                            c_temp = baseColor;
                                break;
                        case "drums":
                            c_temp = drumColor;
                            break;
                        case "piano":
                            c_temp = pianoColor;
                            break;
                        case "vocals":
                            c_temp = vocalsColor;
                            break;
                        case "other":
                            c_temp = otherColor;
                            break;
                    }

                CreateUI_Node(writeNoteList[i].notes[j], i_temp, v_temp,c_temp, new Vector2(posx * j + blankSize * j, posy * i + blankSize * i), new Vector2(width, height));
            }
        }
        #endregion Node 만들기

        RectTransform rt = createNotesRoot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(posx * maxiNodeLength + maxiNodeLength * 20, rt.sizeDelta.y);

    }

    void CreateUI_Node(w_NoteData noteData, int i_index, int v_index ,Color color, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject();
        GameObject go2 = new GameObject("Text");
        RectTransform rt = go.AddComponent<RectTransform>();
        RectTransform rt2 = go2.AddComponent<RectTransform>();
        RawImage ri = go.AddComponent<RawImage>();
        InputField iff = go.AddComponent<InputField>();
        Text t = go2.AddComponent<Text>();
        
        t.font = rslFont;
        t.alignment = TextAnchor.MiddleCenter;
        t.resizeTextForBestFit = true;
        t.color = (Color.white - color) + new Color(0, 0, 0, 1);

        iff.targetGraphic = ri;
        iff.textComponent = t;
        iff.text = noteData.type;
        iff.onEndEdit.AddListener(delegate { NodeClickEvent(noteData, i_index, v_index ,ri, iff); });
        ri.color = color;

        rt.pivot = Vector2.zero;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;

        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        go2.transform.SetParent(go.transform, false);
        rt2.anchorMin = Vector2.zero;
        rt2.anchorMax = Vector2.one;
        rt2.pivot = new Vector2(0.5f, 0.5f);

        go.transform.SetParent(createNotesRoot, false);
        go.SetActive(false);

        vm.targetObjects.Add(go);
    }

    void NodeClickEvent(w_NoteData noteData, int i_Index ,int v_Index, RawImage ri, InputField iff)
    {
        if (iff.text == "R")
            iff.text = "r";
        else if(iff.text == "s"|| iff.text == "l")
            iff.text = iff.text.ToUpper();
        else if(iff.text != "S" && iff.text != "L" && iff.text != "r")
            iff.text = noteData.type;

        if (iff.text.Length != 1)
        {
            if (iff.text.Length > 1)
                iff.text = iff.text.Substring(1, 1);
            else
                iff.text = noteData.type;
        }


        if(iff.text != "r")
        {
            switch (noteData.instrument)
            {
                case "bass":
                    ri.color = baseColor;
                    break;
                case "drums":
                    ri.color = drumColor;
                    break;
                case "piano":
                    ri.color = pianoColor;
                    break;
                case "vocals":
                    ri.color = vocalsColor;
                    break;
                case "other":
                    ri.color = otherColor;
                    break;
            }
        }
        else
        {
            ri.color = nullColor;
        }

        Color color = (Color.white - ri.color) + new Color(0,0,0,1);
        noteData.value = iff.text;
        noteData.type = iff.text;
        iff.textComponent.color = color;
    }


    public void SplitNoteLists(int targetCount)
    {
        if (targetCount <= 0 || targetCount == noteList.Count) return;

        List<NoteList> newNoteLists = new List<NoteList>();

        for (int i = 0; i < targetCount; i++)
        {
            newNoteLists.Add(new NoteList { instrument = $"others_{i + 1}", notes = new List<NoteData>() });
        }

        int index = 0;
        foreach (var noteList in noteList)
        {
            foreach (var note in noteList.notes)
            {
                newNoteLists[index % targetCount].notes.Add(note);
                index++;
            }
        }

        noteList = newNoteLists;
    }

    public void MergeNoteList(int removeIndex)
    {
        if (removeIndex < 0 || removeIndex >= noteList.Count)
        {
            Debug.LogError("Invalid remove index.");
            return;
        }

        NoteList removeList = noteList[removeIndex];
        noteList.RemoveAt(removeIndex);

        int noteCount = removeList.notes.Count;
        int targetListCount = noteList.Count;

        if (targetListCount == 0)
        {
            Debug.LogWarning("No remaining NoteLists to merge into.");
            return;
        }

        int noteIndex = 0;

        // 삭제된 NoteList의 각 Note를 삽입할 위치 찾기
        for (int i = 0; i < noteCount; i++)
        {
            NoteData note = removeList.notes[i];

            if (note.value == "r")
                continue; // 쉼표는 이동하지 않음

            // 각 Note를 삽입할 수 있는 첫 번째 "r"을 가진 NoteList를 찾음
            for (int j = 0; j < targetListCount; j++)
            {
                List<NoteData> targetNotes = noteList[j].notes;

                if (noteIndex < targetNotes.Count && targetNotes[noteIndex].value == "r")
                {
                    targetNotes[noteIndex] = note; // "r" 자리에 삽입
                    break;
                }
            }

            noteIndex++;
        }
    }

    void SaveUJsonButtonEvent()
    {

        u_jsonData.songInfo.tag = tagString.text;
        u_jsonData.songInfo.path = "Songs/" + seletedSongName + ".mp3";
        int LineLength;
        if (int.TryParse(LineCount.text, out LineLength))
            u_jsonData.songInfo.lineCount = LineLength;
        else
            u_jsonData.songInfo.lineCount = 5;

        string saveFilePath = Application.streamingAssetsPath + $"/{seletedSongName}.json";

        if (!File.Exists(saveFilePath))
            File.Create(saveFilePath);

        int[] lineIndexs = new int[u_jsonData.songInfo.lineCount];

        for(int i = 0; i< lineIndexs.Length;i++)
            lineIndexs[i] = 0;

        float durationMinValue = (60.0f / u_jsonData.songInfo.bpm) / 4;

        for (int i = 0; i<maxiNodeLength; i++)
        {
            List<U_Note> notes = new List<U_Note>();

            for(int j = 0; j< u_jsonData.songInfo.lineCount; j++)
            {
                U_Note note = new U_Note();
                float duration = 0;
                if (writeNoteList[j].notes[i].value == "r" || writeNoteList[j].notes[i].value == "r")
                {
                    lineIndexs[j]++;
                    continue;
                }
                else if (lineIndexs[j] > i)
                {
                    continue;
                }
                else if (writeNoteList[j].notes[i].type == "S")
                {
                    lineIndexs[j]++;
                    duration = durationMinValue;
                }
                else
                {
                    for (int z = i; z < writeNoteList[j].notes.Count; z++)
                    {
                        if (writeNoteList[j].notes[z].type == "S" || writeNoteList[j].notes[z].type == "r" || writeNoteList[j].notes[z].value == "r")
                            break;

                        duration += durationMinValue;
                        lineIndexs[j]++;
                    }
                }

                note.line = j;
                note.duration = duration;
                notes.Add(note);
            }
            if(notes.Count > 0)
                u_jsonData.notes.Add(i.ToString(), notes);
        }

        //if (u_jsonData.notes.notes.Count == 0)
        //    ConvertNoteListToUNoteList();

        if (!File.Exists(Application.streamingAssetsPath + "/End_Output.json"))
            File.Create(Application.streamingAssetsPath + "/End_Output.json");

        string TotalJsonData = JsonConvert.SerializeObject(u_jsonData, Formatting.Indented);
        StreamWriter sw = new StreamWriter(Application.streamingAssetsPath + "/End_Output.json");
        sw.WriteLine(TotalJsonData);
        sw.Close();

        Debug.Log("!!!Save Complite!!!");
    }

    void ConvertNoteListToUNoteList()
    {
        int[] lineToNotesLength = new int[noteList.Count];
        for (int i = 0; i < lineToNotesLength.Length; i++)
            lineToNotesLength[i] = noteList[i].notes.Count;

        int noteNumber = 1;
        // i = noteCount
        for(int i = 0; i< maxiNodeLength; i++)
        {
            List<U_Note> tempNote = new List<U_Note>();
            // j == line
            for (int j = 0; j < noteList.Count; j++) 
            {
                //NoteList 안의 notes 리스트의 크기는 각자 다르기 때문에, 
                //LineToNotesLenght와 1:1 매칭되게 한 후,
                //lineToNotesLenght 안의 값이 최대 노트 count와 같거나 크면
                //해당하는 notelist의 길이를 초과한다는 의미이므로 다음 notelist로 이동.
                if (lineToNotesLength[j] <= i)
                    continue;

                NoteData t_noteData = noteList[j].notes[i];

                //notelist의 notes 값이 무음일 경우 다음 notelist로 이동.
                if (t_noteData.value == "r")
                    continue;

                U_Note note = new U_Note();

                note.line = j;

                if(t_noteData.length == "l16" || t_noteData.length == "l8")
                {
                    note.duration = 0f;
                }
                else
                {
                    if (t_noteData.length.Contains("s"))
                    {
                        string duration = t_noteData.length;
                        duration = duration.Split(' ')[1];
                        duration = duration.Replace('(', ' ');
                        duration = duration.Replace(')', ' ');
                        duration = duration.Replace('s', ' ');
                        duration = string.Concat(duration.Where(x => !char.IsWhiteSpace(x)));

                        note.duration = float.Parse(duration);
                    }
                    else
                        note.duration = 1.0f;
                }

                tempNote.Add(note);
            }
            if (tempNote.Count != 0)
            {
                u_jsonData.notes.Add(noteNumber.ToString(), tempNote);
                noteNumber++;
            }
        }
    }

    public float GetMeasureDuration(string timeSignature, int bpm)
    {
        string[] parts = timeSignature.Split('/');
        int beats = int.Parse(parts[0]);   // 분자 (한 마디에 몇 개의 비트)
        return (float)(beats * 60.0) / bpm;  // 마디 길이 (초)
    }



}
[System.Serializable]
public class w_NoteData
{
    public string instrument;
    public string value;
    public string group;
    public string type;
}

[System.Serializable]
public class w_NoteList
{
    public List<w_NoteData> notes;
}


[System.Serializable]
public class NoteData
{
    public string value; 
    public string length;
    public string instrument;
}

[System.Serializable]
public class NoteList
{
    public string instrument;
    public List<NoteData> notes;
}

#region notes

[System.Serializable]
public class U_JSONData
{
    public SongInfo songInfo;
    public GameState gameStats;
    public Dictionary<string, List<U_Note>> notes;
}

public class SongInfo
{
    public string title;
    public int bpm;
    public int lineCount;
    public float duration;
    public string tag;
    public string path;
}

public class GameState
{
    public int maxCombo;
    public float maxPercentage;
}


[System.Serializable]
public class U_NotesData
{
    public Dictionary<string, List<U_Note>> notes;
}

[System.Serializable]
public class U_Note
{
    public int line;
    public float duration;
}

#endregion