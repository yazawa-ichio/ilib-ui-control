# [ilib-ui-control](https://github.com/yazawa-ichio/ilib-ui-control)

Unity UI Control

リポジトリ https://github.com/yazawa-ichio/ilib-ui-control

## 概要

UnityでUIをスタックやキューで管理できるパッケージです。  
1から実装するよりは少し便利なぐらいの機能です。  

## 使用方法(WIP)

### UIのプレハブの設定

まず、UIのプレハブ用にScriptを作成します。  
`UIControl<T>`を継承したクラスをプレハブに`AddComponent`してください。  
`UIControl<T>`はUIの表示・非表示など必要なイベントをoverrideしてフック出来ます。  
自身を閉じる場合は`Close`関数で簡単に閉じることが出来ます。  
作成したUIは`Resources`フォルダに配置します。  

例
```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using ILib.UI;

namespace App
{
	public class SampleUIParam
	{
		public Action OnExceute;
		public Action OnClose;
	}
	//パラメーターを指定できる。
	public class SampleUIControl : UIControl<SampleUIParam>
	{
		[SerializeField]
		Button m_ExceuteButton;
		[SerializeField]
		Button m_CloseButton;

		// UIが作成された直後に呼び出されます。
		protected override Task OnCreated(SampleUIParam prm)
		{
			m_ExceuteButton.onClick.AddListener(prm.OnExceute);
			m_CloseButton.onClick.AddListener(OnCloseButton);
			return base.OnCreated(prm);
		}

		void OnCloseButton()
		{
			//Paramでも取り出せる
			Param.OnClose();
			//自身を閉じる場合はCloseボタンを実行する
			Close();
		}
	}
}
```

### UIのControllerを設定する

UIのControllerは`UIStack`と`UIQueue`の二種類あります。
それぞれ`UIStack`はPush・Pop・Switch操作、`UIQueue`はEnqueue・Close操作が行えます。
基本的なUIは`UIStack`を利用し、`UIQueue`はダイヤログなど表示が終わるまで操作を出来ないようにする場合に利用します。
継承を行うことでロード方法を変えたり、共通処理を仕込んだりより細かい制御が可能になります。

#### パラメーターやコントローラーを制限する

`UIStack<TParam, UControl>`と`UIQueue<TParam, UControl>`を継承する事で基底のパラメーターやコントローラーを明示的に宣言できます。  

例えばMVVMなどのデータバインディングを行うのであれば、パラメーターはViewMovelのため以下の用に宣言します。  

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using ILib.UI;

namespace App
{
	public class SampleViewModel : IViewModel
	{
		//パラメーター
		public string Message { get; set; }
	}

	public class ViewControl : UIControl<IViewModel>
	{
		IView m_View;
		protected sealed override Task OnCreated(IViewModel prm)
		{
			//Viewを取得
			m_View = GetComponent<IView>();
			//データバインディングを実行
			m_View.Attach(prm);
			return OnCreatedImpl();
		}
	}

	public class MVVMUIStack : UIStack<IViewModel, IControl<IViewModel>>
	{
		CanvasGroup m_CanvasGroup;
		private void Awake()
		{
			m_CanvasGroup = GetComponent<CanvasGroup>();
		}
		// 処理中（アニメーション中）などレイキャストを切る
		protected override void OnStartProcess()
		{
			m_CanvasGroup.blocksRaycasts = false;
		}
		protected override void OnEndProcess()
		{
			m_CanvasGroup.blocksRaycasts = true;
		}
	}

	public class Application
	{
		MVVMUIStack m_UIStack;
		void Prop()
		{
			var vm = new SampleViewModel();
			vm.Message = "Message";
			m_UIStack.Push(path:"UI/Exsample", prm: vm);
		}
	}

}

```

## LICENSE

https://github.com/yazawa-ichio/ilib-ui-control/blob/master/LICENSE