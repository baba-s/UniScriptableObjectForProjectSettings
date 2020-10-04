using JetBrains.Annotations;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
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
		// .asset から読み込んだ ScriptableObject のインスタンスをキャッシュするための変数
		private static T m_instance;

		//================================================================================
		// プロパティ(static)
		//================================================================================
		private static string AssetPath => $"ProjectSettings/Kogane/{typeof( T ).Name}.asset";

		//================================================================================
		// 関数(static)
		//================================================================================
		/// <summary>
		/// 指定された .asset から ScriptableObject のインスタンスを読み込んで返します
		/// .asset が存在しない場合は ScriptableObject のインスタンスを新規作成して返します
		/// </summary>
		public static T GetInstance()
		{
			// 既にインスタンスを作成もしくは読み込み済みの場合はそれを返す
			if ( m_instance != null ) return m_instance;

			// .asset が存在しない場合はインスタンスを新規作成する
			if ( !File.Exists( AssetPath ) )
			{
				m_instance = CreateInstance<T>();
				return m_instance;
			}

			// .asset が存在する場合は .asset を読み込む
			m_instance = InternalEditorUtility
					.LoadSerializedFileAndForget( AssetPath )
					.OfType<T>()
					.FirstOrDefault()
				;

			// .asset が不正な形式で読み込むことができなかった場合は
			// インスタンスを新規作成する
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
			[CanBeNull] Action<SerializedObject> onGUI                = null,
			[CanBeNull] Action<SerializedObject> onGUIExtra           = null
		)
		{
			if ( settingsProviderPath == null )
			{
				settingsProviderPath = $"Kogane/{typeof( T ).Name}";
			}

			// ScriptableObject のインスタンスを新規作成もしくは .asset から読み込む
			// ScriptableObject の GUI を表示する SettingsProvider を作成する
			var instance         = GetInstance();
			var serializedObject = new SerializedObject( instance );
			var keywords         = SettingsProvider.GetSearchKeywordsFromSerializedObject( serializedObject );
			var provider         = new SettingsProvider( settingsProviderPath, SettingsScope.Project, keywords );

			provider.guiHandler += _ => OnGuiHandler( onGUI, onGUIExtra );

			return provider;
		}

		/// <summary>
		/// SettingsProvider の GUI を描画する時に呼び出されます
		/// </summary>
		private static void OnGuiHandler
		(
			Action<SerializedObject> onGUI,
			Action<SerializedObject> onGUIExtra
		)
		{
			var instance = GetInstance();
			var editor   = Editor.CreateEditor( instance );

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

				onGUIExtra?.Invoke( serializedObject );

				if ( !scope.changed ) return;

				// パラメータが編集された場合は インスタンスに反映して
				// なおかつ .asset ファイルとしても保存する
				serializedObject.ApplyModifiedProperties();

				var directoryPath = Path.GetDirectoryName( AssetPath );

				if ( !string.IsNullOrWhiteSpace( directoryPath ) )
				{
					Directory.CreateDirectory( directoryPath );
				}

				InternalEditorUtility.SaveToSerializedFileAndForget
				(
					obj: new[] { editor.target },
					path: AssetPath,
					allowTextSerialization: true
				);
			}
		}
	}
}