﻿using PowerArgs.Cli;
using PowerArgs;
using PowerArgs.Cli.Physics;
using System;
using System.Linq;

namespace ConsoleZombies
{
    public class GameApp : ConsoleApp
    {
        public GameInputManager InputManager { get { return Get<GameInputManager>(); } set { Set(value); } }
        private HeadsUpDisplay headsUpDisplay;
        private ScenePanel scenePanel;
        private bool implicitPause = false;

        public MainCharacter MainCharacter { get { return Get<MainCharacter>(); } set { Set(value); } }

        public Scene GameScene
        {
            get
            {
                return scenePanel.Scene;
            }
        }

        public GameApp()
        {
            this.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Escape, null, () =>
            {
                implicitPause = true;
                scenePanel.Scene.Stop();

                Dialog.ConfirmYesOrNo("Are you sure you want to quit?", () =>
                {
                    Stop();
                }, ()=>
                {
                    scenePanel.Scene.Start();
                });
            }, this.LifetimeManager);


            var borderPanel = LayoutRoot.Add(new ConsolePanel() { Background = ConsoleColor.DarkGray, Width = LevelDefinition.Width + 2, Height = LevelDefinition.Height + 2 }).CenterHorizontally().CenterVertically();
            scenePanel = borderPanel.Add(new ScenePanel(LevelDefinition.Width, LevelDefinition.Height)).Fill(padding: new Thickness(1, 1, 1, 1));
            InputManager = new GameInputManager(scenePanel.Scene, this);
            headsUpDisplay = LayoutRoot.Add(new HeadsUpDisplay(this) { Width = LevelDefinition.Width }).DockToBottom().CenterHorizontally();
            LayoutRoot.Add(new FramerateControl(scenePanel.Scene));
            QueueAction(() => { scenePanel.Scene.Start(); });

            GameScene.Started.SubscribeForLifetime(() =>
            {
                SoundEffects.Instance.SoundThread.Start();
                SoundEffects.Instance.PlaySound("music");
            }, this.LifetimeManager);

            scenePanel.Scene.Stopped.SubscribeForLifetime(() =>
            {
                SoundEffects.Instance.SoundThread.Stop();
                if (this.IsRunning && implicitPause == false)
                {
                    QueueAction(() =>
                    {
                        Dialog.ShowMessage("Paused", () => { scenePanel.Scene.Start(); });
                    });
                }
                implicitPause=false;
            }, this.LifetimeManager);
        }

        public void Load(LevelDefinition def)
        {
            scenePanel.Scene.QueueAction(() =>
            {
                Inventory toKeep = null;
                if(MainCharacter.Current != null)
                {
                    toKeep = MainCharacter.Current.Inventory;
                }

                scenePanel.Scene.Clear();
                def.Hydrate(scenePanel.Scene, false);

                foreach (var portal in scenePanel.Scene.Things.Where(p => p is Portal).Select(p => p as Portal))
                {
                    var localPortal = portal;
                    localPortal.PortalEntered.SubscribeForLifetime(()=>
                    {
                        Load(LevelDefinition.Load(localPortal.DestinationId));
                    }, portal.LifetimeManager);
                }

                if(MainCharacter.Current != null)
                {
                    this.MainCharacter = MainCharacter.Current;
                    if (toKeep != null)
                    {
                        MainCharacter.Current.Inventory = toKeep;
                    }
                    InputManager.SetKeyMap(InputManager.KeyMap);
                    MainCharacter.Current.EatenByZombie.SubscribeForLifetime(() =>
                    {
                        implicitPause = true;
                        scenePanel.Scene.Stop();
                        QueueAction(() =>
                        {
                            SoundEffects.Instance.PlaySound("playerdead");
                            
                            Dialog.ShowMessage("Game over :(",()=> 
                            {
                                Stop();
                            });
                        });
                    },scenePanel.LifetimeManager);
                }
            });
        }

        public void ShowMenu()
        {
            if (ConsoleApp.Current == this)
            {
                ShowMenuInternal();
            }
            else
            {
                QueueAction(ShowMenuInternal);
            }
        }

        private void ShowMenuInternal()
        {
            implicitPause = true;
            GameScene.TogglePause();
            Dialog.Pick("Game Menu".ToConsoleString(), new DialogOption[]
            {
                new DialogOption() { Id = "controls", DisplayText = "Kayboard Controls".ToConsoleString() },
                new DialogOption() { Id = "options", DisplayText = "Game Options".ToConsoleString() },
                new DialogOption() { Id = "help", DisplayText = "Help".ToConsoleString() },
                new DialogOption() { Id = "about", DisplayText = "About".ToConsoleString() },
            }).Then((menuItem) =>
            {
                if (menuItem == null) return;
                if(menuItem.Id == "controls")
                {
                    var editor = new KeyMapEditor(this.InputManager.KeyMap) { Y = 3 };
                    var scrollPanel = new ScrollablePanel();
                    scrollPanel.ScrollableContent.Add(editor);
                    var dialog = new Dialog(scrollPanel);
                    dialog.Show().Then(() => 
                    {
                        GameScene.QueueAction(() => { InputManager.SetKeyMap(InputManager.KeyMap); });
                        
                        GameScene.Start();
                    });

                }
            }).Finally((p)=>
            {

            });
        }
    }
}
