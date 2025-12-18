using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using WakfuMod.jugador;
using System.Collections.Generic;
using Terraria.Audio;

namespace WakfuMod.ModSystems
{
    public class WakfuUISystem : ModSystem
    {
        internal BalanceToggleUI balanceToggleUI;
        internal UserInterface balanceToggleInterface;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                balanceToggleUI = new BalanceToggleUI();
                balanceToggleUI.Activate();
                balanceToggleInterface = new UserInterface();
                balanceToggleInterface.SetState(balanceToggleUI);
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (balanceToggleInterface != null && Main.playerInventory)
            {
                balanceToggleInterface.Update(gameTime);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryIndex != -1)
            {
                layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
                    "WakfuMod: Balance Toggle",
                    delegate
                    {
                        if (Main.playerInventory)
                        {
                            balanceToggleInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }

    public class BalanceToggleUI : UIState
    {
        public UIPanel toggleButton;
        public UIText toggleText;

        // Variables para arrastrar
        private bool dragging = false;
        private Vector2 offset;
        private bool isLeftDrag = false;
        private Vector2 dragStartPos;

        public override void OnInitialize()
        {
            toggleButton = new UIPanel();
            toggleButton.SetPadding(0);
            toggleButton.Width.Set(30, 0f);
            toggleButton.Height.Set(30, 0f);
            toggleButton.BackgroundColor = Color.Red;
            toggleButton.BorderColor = Color.Black;
            
            // Posicionamiento inicial (se ajustará en Update para seguir la basura)
            toggleButton.Left.Set(370, 0f);
            toggleButton.Top.Set(300, 0f);

            // --- Eventos de Arrastre y Clic ---
            // Reemplazamos OnLeftClick simple por lógica de arrastre/clic
            toggleButton.OnLeftMouseDown += (evt, element) => {
                offset = new Vector2(evt.MousePosition.X - toggleButton.Left.Pixels, evt.MousePosition.Y - toggleButton.Top.Pixels);
                dragStartPos = evt.MousePosition;
                dragging = true;
                isLeftDrag = true;
            };
            
            toggleButton.OnLeftMouseUp += (evt, element) => {
                dragging = false;
                // Si soltamos el clic izquierdo y no nos hemos movido mucho, es un clic normal -> Toggle
                if (isLeftDrag && Vector2.Distance(evt.MousePosition, dragStartPos) < 5f)
                {
                    ToggleButton_OnClick(evt, element);
                }
                isLeftDrag = false;
            };
            
            // Arrastre con clic derecho
            toggleButton.OnRightMouseDown += (evt, element) => {
                offset = new Vector2(evt.MousePosition.X - toggleButton.Left.Pixels, evt.MousePosition.Y - toggleButton.Top.Pixels);
                dragging = true;
                isLeftDrag = false;
            };
            
            toggleButton.OnRightMouseUp += (evt, element) => {
                dragging = false;
            };

            Append(toggleButton);

            // --- Texto encima del botón ---
            toggleText = new UIText("");
            toggleText.HAlign = 0.5f; // Centrar horizontalmente respecto al botón
            toggleText.Top.Set(-25f, 0f); // Posicionar encima
            toggleButton.Append(toggleText);
        }

        private void ToggleButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            Player player = Main.LocalPlayer;
            WakfuPlayer wakfuPlayer = player.GetModPlayer<WakfuPlayer>();
            wakfuPlayer.ToggleBalanceMode();
            SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Lógica de arrastre
            if (dragging)
            {
                toggleButton.Left.Set(Main.MouseScreen.X - offset.X, 0f);
                toggleButton.Top.Set(Main.MouseScreen.Y - offset.Y, 0f);
                Recalculate();
            }

            // Actualizar color y texto según el estado
            Player player = Main.LocalPlayer;
            WakfuPlayer wakfuPlayer = player.GetModPlayer<WakfuPlayer>();
            
            // --- NUEVO: Check para deshabilitar visualmente ---
            if (wakfuPlayer.claseElegida == WakfuClase.Xelor || wakfuPlayer.claseElegida == WakfuClase.Hipermago)
            {
                 toggleButton.BackgroundColor = Color.Gray;
                 toggleText.SetText("Disabled");
                 return; 
            }
            
            if (wakfuPlayer.BalanceMode)
            {
                toggleButton.BackgroundColor = Color.Green;
                toggleText.SetText("Vanilla dmg");
            }
            else
            {
                toggleButton.BackgroundColor = Color.Red;
                toggleText.SetText("Weapons deal maxHP% dmg");
            }
        }
    }
}
