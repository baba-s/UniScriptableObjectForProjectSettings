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
		// 関数(static)
		//================================================================================
		/// <summary>
		/// 指定された .asset から ScriptableObject のインスタンスを読み込んで返します
		/// .asset が存在しない場合は ScriptableObject のインスタンスを新規作成して返します
		/// </summary>
		private static T CreateOrLoadFromAsset( string assetPath )
		{
			// 既にインスタンスを作成もしくは読み込み済みの場合はそれを返す
			if ( m_instance != null ) return m_instance;

			// .asset が存在しない場合はインスタンスを新規作成する
			if ( !File.Exists( assetPath ) )
			{
				m_instance = CreateInstance<T>();
				return m_instance;
			}

			// .asset が存在する場合は .asset を読み込む
			m_instance = InternalEditorUtility
					.LoadSerializedFileAndForget( assetPath )
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
			[CanBeNull] string                   assetPath            = null,
			[CanBeNull] Action<SerializedObject> onGUI                = null
		)
		{
			// Project Settings のパスが指定されていない場合はデフォルト値を使用する
			if ( settingsProviderPath == null )
			{
				settingsProviderPath = $"Kogane/{typeof( T ).Name}";
			}

			// .asset のファイルパスが指定されていない場合はデフォルト値を使用する
			if ( assetPath == null )
			{
				assetPath = $"ProjectSettings/Kogane/{typeof( T ).Name}.asset";
			}

			// ScriptableObject のインスタンスを新規作成もしくは .asset から読み込む
			// ScriptableObject の GUI を表示する SettingsProvider を作成する
			var instance         = CreateOrLoadFromAsset( assetPath );
			var serializedObject = new SerializedObject( instance );
			var keywords         = SettingsProvider.GetSearchKeywordsFromSerializedObject( serializedObject );
			var editor           = Editor.CreateEditor( instance );
			var provider         = new SettingsProvider( settingsProviderPath, SettingsScope.Project, keywords );

			provider.guiHandler += _ => OnGuiHandler( editor, assetPath, onGUI );

			return provider;
		}

		/// <summary>
		/// SettingsProvider の GUI を描画する時に呼び出されます
		/// </summary>
		private static void OnGuiHandler
		(
			Editor                   editor,
			string                   assetPath,
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
				// なおかつ .asset ファイルとしても保存する
				serializedObject.ApplyModifiedProperties();

				var directoryPath = Path.GetDirectoryName( assetPath );

				if ( !string.IsNullOrWhiteSpace( directoryPath ) )
				{
					Directory.CreateDirectory( directoryPath );
				}

				InternalEditorUtility.SaveToSerializedFileAndForget
				(
					obj: new[] { editor.target },
					path: assetPath,
					allowTextSerialization: true
				);
			}
		}
	}
}