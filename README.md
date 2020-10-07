# UniScriptableObjectForProjectSettings

Project Settings に簡単にメニューを追加できるエディタ拡張

## 使い方

```cs
using Kogane;
using UnityEditor;
using UnityEngine;

public class MySettings : ScriptableObjectForProjectSettings<MySettings>
{
    [SerializeField] private int    m_id   = 25;
    [SerializeField] private string m_name = "ピカチュウ";

    public int    Id   => m_id;
    public string Name => m_name;

    // Project Settings のメニューに登録
    [SettingsProvider]
    private static SettingsProvider SettingsProvider()
    {
        return CreateSettingsProvider
        (
            settingsProviderPath: "MyProject/MySettings", // メニューの名前を指定できます
            onGUI: null,                                  // メニューの GUI を上書きしたい場合は指定します
            onGUIExtra: null                              // メニューの末尾に GUI を追加したい場合は指定します
        );
    }
}
```

例えば上記のようなクラスを作成すると  

![2020-10-07_200521](https://user-images.githubusercontent.com/6134875/95323196-bce4c700-08d8-11eb-9840-800d04269d72.png)

Project Setting にこのようにメニューを追加できます  

```cs
using UnityEditor;
using UnityEngine;

public class Example
{
    [MenuItem( "Tools/Hoge" )]
    private static void Hoge()
    {
        var settings = MySettings.GetInstance();

        Debug.Log( settings.Id );
        Debug.Log( settings.Name );
    }
}
```

メニューの設定にアクセスしたい場合はこのようなコードを記述します  

設定は Unity プロジェクトの「ProjectSettings/Kogane」フォルダ以下に保存されます
