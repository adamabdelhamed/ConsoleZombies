﻿using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;

namespace ConsoleZombies
{
    public class WeaponRow
    {
        public ConsoleString Weapon { get; set; }
        public ConsoleString Trigger { get; set; }
        public ConsoleString Amount { get; set; }
    }

    public class HeadsUpDisplay : ConsolePanel
    {
        private GameApp gameApp;
        private Label messageLabel;
        private ConsolePanel messagePanel;
        public HeadsUpDisplay(GameApp app)
        {
            this.gameApp = app;
            this.Height = 7;

            var topPanel = Add(new StackPanel() { Orientation = Orientation.Horizontal, Height = 6 }).FillHoriontally();
            var leftPanel = topPanel.Add(new StackPanel() { Orientation = Orientation.Vertical, Width = 12 }).FillVertically();
            var middleGrid = topPanel.Add(new Grid(new List<object>()
            {
                new WeaponRow { Weapon = "".ToWhite(), Trigger= "".ToWhite(), Amount="".ToWhite() },
                new WeaponRow { Weapon = "".ToWhite(), Trigger= "".ToWhite(), Amount="".ToWhite() },
            }) { Gutter=0, ShowEndIfComplete=false, CanFocus=false, Width = 35 }).FillVertically();
            messagePanel = topPanel.Add(new ConsolePanel() { IsVisible=false, Width = 31, Background = ConsoleColor.White }).FillVertically(padding: new Thickness(0,0,1,1));
            var bottomPanel = Add(new StackPanel() { Orientation = Orientation.Horizontal, Margin=2, Height = 1 }).FillHoriontally().DockToBottom();

            var hpLabel = leftPanel.Add(new Label() { Text = "HP".ToGray() }).FillHoriontally();
            var hpValue = leftPanel.Add(new Label() { Text = ConsoleString.Empty }).FillHoriontally();
            var spacer = leftPanel.Add(new Label() { Text = ConsoleString.Empty });
            var aimLabel = leftPanel.Add(new Label() { Text = "AIM".ToGray() }).FillHoriontally();
            var aimValue = leftPanel.Add(new Label() { Text = "".ToWhite() }).FillHoriontally();

            middleGrid.VisibleColumns[0].ColumnDisplayName = new ConsoleString(middleGrid.VisibleColumns[0].ColumnDisplayName.ToString(), ConsoleColor.Gray);
            middleGrid.VisibleColumns[1].ColumnDisplayName = new ConsoleString(middleGrid.VisibleColumns[1].ColumnDisplayName.ToString(), ConsoleColor.Gray);
            middleGrid.VisibleColumns[2].ColumnDisplayName = new ConsoleString(middleGrid.VisibleColumns[2].ColumnDisplayName.ToString(), ConsoleColor.Gray);

            middleGrid.VisibleColumns[0].OverflowBehavior = new TruncateOverflowBehavior() { TruncationText = "", ColumnWidth = 15 };
            middleGrid.VisibleColumns[1].OverflowBehavior = new TruncateOverflowBehavior() { TruncationText = "", ColumnWidth = 10 };
            middleGrid.VisibleColumns[2].OverflowBehavior = new TruncateOverflowBehavior() { TruncationText = "", ColumnWidth = 10};
            var menuLabel = bottomPanel.Add(new Label() { Text = "".ToYellow() });
            var pauseLabel = bottomPanel.Add(new Label() { Text = "".ToYellow() });
            var quitLabel = bottomPanel.Add(new Label() { Text = "Quit [ESC]".ToYellow() });

            messageLabel = messagePanel.Add(new Label() { Mode = LabelRenderMode.MultiLineSmartWrap, Background = ConsoleColor.White, Text = "".ToBlack(bg: ConsoleColor.White)}).Fill(padding: new Thickness(1, 1, 1, 1));

            app.SubscribeProxiedForLifetime(app, nameof(app.MainCharacter) + "." + nameof(MainCharacter.HealthPoints), () =>
            {
                var hp = app.MainCharacter?.HealthPoints;
                hpValue.Text = hp.HasValue ? FormatHPValue(hp.Value) : "unknown".ToRed();
            }, this.LifetimeManager);


            app.SynchronizeProxiedForLifetime(app, nameof(app.InputManager) + "." + nameof(GameInputManager.KeyMap) + "." + nameof(KeyMap.MenuKey), () =>
            {
                menuLabel.Text = $"Menu [{app.InputManager.KeyMap.MenuKey}]".ToYellow();
            }, this.LifetimeManager);

            app.SynchronizeProxiedForLifetime(app, nameof(app.InputManager) + "." + nameof(GameInputManager.KeyMap) + "." + nameof(KeyMap.TogglePauseKey), () =>
            {
                pauseLabel.Text = $"Pause [{app.InputManager.KeyMap.TogglePauseKey}]".ToYellow();
            }, this.LifetimeManager);

            app.SynchronizeProxiedForLifetime(app, nameof(app.InputManager) + "." + nameof(GameInputManager.KeyMap) + "." + ObservableObject.AnyProperty, () =>
            {
                var primaryWeaponRow = ((WeaponRow)(middleGrid.DataSource as MemoryDataSource).Items[0]);
                primaryWeaponRow.Trigger = $"[{this.gameApp.InputManager.KeyMap.PrimaryWeaponKey}]".ToWhite();

                var explosiveWeaponRow = ((WeaponRow)(middleGrid.DataSource as MemoryDataSource).Items[1]);
                explosiveWeaponRow.Trigger = $"[{this.gameApp.InputManager.KeyMap.ExplosiveWeaponKey}]".ToWhite();


                aimLabel.Text = $"Aim[{this.gameApp.InputManager.KeyMap.AimToggleKey}]".ToGray();
            }, this.LifetimeManager);

            app.SynchronizeProxiedForLifetime(app, nameof(app.MainCharacter)+"."+nameof(MainCharacter.Inventory)+"."+nameof(Inventory.PrimaryWeapon), () =>
            {
                var row = ((WeaponRow)(middleGrid.DataSource as MemoryDataSource).Items[0]);
                var weaponName = app.MainCharacter?.Inventory?.PrimaryWeapon?.GetType().Name;
                row.Weapon = weaponName != null ? weaponName.ToWhite() : "none".ToRed();
                if (weaponName != null)
                {
                    ShowMessage("Equipped ".ToBlack() + weaponName.ToDarkBlue(), TimeSpan.FromSeconds(4));
                }
            }, this.LifetimeManager);

            app.SynchronizeProxiedForLifetime(app, 
                nameof(app.MainCharacter) + "." + 
                nameof(MainCharacter.Inventory) + "." + 
                nameof(Inventory.PrimaryWeapon) + "." +
                nameof(Weapon.AmmoAmount), () =>
            {
                var row = ((WeaponRow)(middleGrid.DataSource as MemoryDataSource).Items[0]);
                var ammo = app.MainCharacter?.Inventory?.PrimaryWeapon?.AmmoAmount;
                row.Amount = ammo.HasValue ? FormatAmmoAmmount(ammo.Value): "empty".ToRed();
            }, this.LifetimeManager);

            app.SynchronizeProxiedForLifetime(app, nameof(app.MainCharacter) + "." + nameof(MainCharacter.Inventory) + "." + nameof(Inventory.ExplosiveWeapon), () =>
            {
                var row = ((WeaponRow)(middleGrid.DataSource as MemoryDataSource).Items[1]);
                var weaponName = app.MainCharacter?.Inventory?.ExplosiveWeapon?.GetType().Name;
                row.Weapon = weaponName != null ? weaponName.ToWhite() : "none".ToRed();
                if (weaponName != null)
                {
                    ShowMessage("Equipped ".ToBlack() + weaponName.ToDarkBlue(), TimeSpan.FromSeconds(4));
                }
            }, this.LifetimeManager);

            app.SynchronizeProxiedForLifetime(app,
                nameof(app.MainCharacter) + "." +
                nameof(MainCharacter.Inventory) + "." +
                nameof(Inventory.ExplosiveWeapon) + "." +
                nameof(Weapon.AmmoAmount), () =>
                {
                    var row = ((WeaponRow)(middleGrid.DataSource as MemoryDataSource).Items[1]);
                    var ammo = app.MainCharacter?.Inventory?.ExplosiveWeapon?.AmmoAmount;
                    row.Amount = ammo.HasValue ? FormatAmmoAmmount(ammo.Value) : "empty".ToRed();
                }, this.LifetimeManager);



            app.SynchronizeProxiedForLifetime(app, nameof(app.MainCharacter) + "." + nameof(MainCharacter.AimMode), () =>
            {
                var aimMode = app.MainCharacter?.AimMode;
                aimValue.Text = aimMode.HasValue ? aimMode.Value.ToString().ToConsoleString(aimMode.Value == AimMode.Auto ? ConsoleColor.White : ConsoleColor.Cyan) : "".ToConsoleString();
            }, this.LifetimeManager);
        }

        public void ShowMessage(ConsoleString message, TimeSpan? time = null)
        {
            message = message.ToDifferentBackground(ConsoleColor.White);
            messageLabel.Text = message;
            messagePanel.IsVisible = true;
            if(time.HasValue)
            {
                messageLabel.GetPropertyValueLifetime(nameof(Label.Text)).LifetimeManager
                    .Manage(Application.SetTimeout(() =>
                {
                    messageLabel.Text = ConsoleString.Empty;
                    messagePanel.IsVisible = false;
                }, time.Value));
            }
        }

        private ConsoleString FormatAmmoAmmount(int amount)
        {
            if(amount > 10)
            {
                return (amount + "").ToGreen();
            }
            else if(amount > 0 )
            {
                return (amount + "").ToYellow();
            }
            else
            {
                return "empty".ToRed();
            }
        }

        private ConsoleString FormatHPValue(float hp)
        {
            hp = (int)Math.Ceiling(hp);

            if (hp >= 60)
            {
                return (hp + "").ToGreen();
            }
            else if (hp >= 30)
            {
                return (hp + "").ToYellow();
            }
            else
            {
                return (hp + "").ToRed();
            }
        }
    }
}
