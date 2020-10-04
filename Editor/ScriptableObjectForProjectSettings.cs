using System;
using JetBrains.Annotations;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Kogane
{
	/// <summary>
	/// Project Settings に表示する ScriptableObject を管理するクラス
	/// </summary>
	public abstract class ScriptableObjectForProjectSettings<T> : ScriptableObject
		where T : ScriptableObjectForProjectSettings<T>
	{
		//================================================================================
		// 変数(static)
		//================================================================================
		// JSON から読み込んだ ScriptableObject のインスタンスをキャッシュするための変数
		private static T m_instance;

		//================================================================================
		// 関数(static)
		//================================================================================
		/// <summary>
		/// 指定された JSON から ScriptableObject のインスタンスを読み込んで返します
		/// JSON が存在しない場合は ScriptableObject のインスタンスを新規作成して返します
		/// </summary>
		private static T CreateOrLoadFromJson( string jsonPath )
		{
			// 既にインスタンスを作成もしくは読み込み済みの場合はそれを返す
			if ( m_instance != null ) return m_instance;

			// インスタンスを作成する
			m_instance = CreateInstance<T>();

			// JSON が存在しない場合は新規作成したインスタンスを返す
			if ( !File.Exists( jsonPath ) ) return m_instance;

			// JSON が存在する場合は JSON を読み込んで
			// 作成したインスタンスのパラメータに上書きする
			var json = File.ReadAllText( jsonPath, Encoding.UTF8 );
			JsonUtility.FromJsonOverwrite( json, m_instance );

			// JSON が不正な形式で読み込むことができなかった場合は
			// 再度インスタンスを新規作成する
			if ( m_instance == null )
			{
				m_instance = CreateInstance<T>();
			}

			return m_instance;
		}

		/// <summary>
		/// Project Settings に表示する SettingsProvider を作成して返します
		/// </summary>
		public static SettingsProvider CreateSettingsProvider
		(
			[CanBeNull] string                   settingsProviderPath = null,
			[CanBeNull] string                   jsonPath             = null,
			[CanBeNull] Action<SerializedObject> onGUI                = null
		)
		{
			// Project Settings のパスが指定されていない場合はデフォルト値を使用する
			if ( settingsProviderPath == null )
			{
				settingsProviderPath = $"Kogane/{typeof( T ).Name}";
			}

			// JSON のファイルパスが指定されていない場合はデフォルト値を使用する
			if ( jsonPath == null )
			{
				jsonPath = $"ProjectSettings/Kogane/{typeof( T ).Name}.json";
			}

			// ScriptableObject のインスタンスを新規作成もしくは JSON から読み込み
			// ScriptableObject の GUI を表示する SettingsProvider を作成する
			var instance         = CreateOrLoadFromJson( jsonPath );
			var serializedObject = new SerializedObject( instance );
			var keywords         = SettingsProvider.GetSearchKeywordsFromSerializedObject( serializedObject );
			var editor           = Editor.CreateEditor( instance );
			var provider         = new SettingsProvider( settingsProviderPath, SettingsScope.Project, keywords );

			provider.guiHandler += _ => OnGuiHandler( editor, jsonPath, onGUI );

			return provider;
		}

		/// <summary>
		/// SettingsProvider の GUI を描画する時に呼び出されます
		/// </summary>
		private static void OnGuiHandler
		(
			Editor                   editor,
			string                   jsonPath,
			Action<SerializedObject> onGUI
		)
		{
			using ( var scope = new EditorGUI.ChangeCheckScope() )
			{
				var serializedObject = editor.serializedObject;

				serializedObject.Update();

				// onGUI が指定されている場合はそれを描画する
				if ( onGUI != null )
				{
					onGUI( serializedObject );
				}
				else
				{
					// onGUI が指定されていない場合はデフォルトの Inspector を描画する
					editor.DrawDefaultInspector();
				}

				if ( !scope.changed ) return;

				// パラメータが編集された場合は インスタンスに反映して
				// なおかつ JSON ファイルとしても保存する
				serializedObject.ApplyModifiedProperties();

				var json          = JsonUtility.ToJson( editor.target, true );
				var directoryPath = Path.GetDirectoryName( jsonPath );

				if ( !string.IsNullOrWhiteSpace( directoryPath ) )
				{
					Directory.CreateDirectory( directoryPath );
				}

				File.WriteAllText( jsonPath, json, Encoding.UTF8 );
			}
		}
	}
}