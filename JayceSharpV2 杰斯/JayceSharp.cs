﻿using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
/*
 * ToDo:
 * Q doesnt shoot much < fixed
 * Full combo burst <-- done
 * Useles gate <-- fixed
 * Add Fulldmg combo starting from hammer <-- done
 * Auto ignite if killabe/burst <-- done
 * More advanced Q calc area on hit
 * MuraMune support <-- done
 * Auto gapclosers E <-- done
 * GhostBBlade active <-- done
 * packet cast E <-- done 
 * 
 * 
 * Auto ks with QE <-done
 * Interupt channel spells <-done
 * Omen support <- done
 * 
 * 
 * */
using SharpDX;
using Color = System.Drawing.Color;


namespace JayceSharpV2
{
    internal class JayceSharp
    {
        public const string CharName = "Jayce";

        public static Menu Config;

        public static HpBarIndicator hpi = new HpBarIndicator();

        public JayceSharp()
        {
            /* CallBAcks */
            CustomEvents.Game.OnGameLoad += onLoad;
        }

        private static void onLoad(EventArgs args)
        {

            Game.PrintChat("Jayce - SharpV2 by DeTuKs 鍔犺浇鎴愬姛!姹夊寲by浜岀嫍!QQ缇361630847");
            Jayce.setSkillShots();
            try
            {

                Config = new Menu("JayceV2-浜岀嫍姹夊寲", "Jayce", true);
                //Orbwalker
                Config.AddSubMenu(new Menu("璧扮爫", "Orbwalker"));
                Jayce.orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));
                //TS
                Menu targetSelectorMenu = new Menu("鐩爣閫夋嫨", "Target Selector");
                SimpleTs.AddToMenu(targetSelectorMenu);
                Config.AddSubMenu(targetSelectorMenu);
                //Combo
                Config.AddSubMenu(new Menu("杩炴嫑 鐘€鍒﹟", "combo"));
                Config.SubMenu("combo").AddItem(new MenuItem("comboItems", "浣跨敤鏆楃ず")).SetValue(true);
                Config.SubMenu("combo").AddItem(new MenuItem("hammerKill", "鍙互鏉€姝诲垏鎹㈤敜瀛恷")).SetValue(true);
                Config.SubMenu("combo").AddItem(new MenuItem("fullDMG", "鍋氬畬鏁寸殑浼ゅ")).SetValue(new KeyBind('A', KeyBindType.Press));
                Config.SubMenu("combo").AddItem(new MenuItem("injTarget", "濉旀敞鍏")).SetValue(new KeyBind('G', KeyBindType.Press));
                Config.SubMenu("combo").AddItem(new MenuItem("awsPress", "鏉版柉瑁哥収!!")).SetValue(new KeyBind('Z', KeyBindType.Press));

                //Extra
                Config.AddSubMenu(new Menu("鏉傞」 鐘€鍒﹟", "extra"));
                Config.SubMenu("extra").AddItem(new MenuItem("shoot", "鎵嬪姩EQ浜岃繛")).SetValue(new KeyBind('T', KeyBindType.Press));
                Config.SubMenu("extra").AddItem(new MenuItem("gapClose", "鑷姩閿ゅ紑绐佽劯鐨剕")).SetValue(true);
                Config.SubMenu("extra").AddItem(new MenuItem("autoInter", "Interupt spells")).SetValue(true);
                Config.SubMenu("extra").AddItem(new MenuItem("useMunions", "鑷姩EQ鏁屾柟")).SetValue(true);
                Config.SubMenu("extra").AddItem(new MenuItem("killSteal", "鎶汉澶磡")).SetValue(false);
                Config.SubMenu("extra").AddItem(new MenuItem("packets", "浣跨敤灏佸寘")).SetValue(false);

                //Debug
                Config.AddSubMenu(new Menu("鑼冨洿缁樺埗", "draw"));
                Config.SubMenu("draw").AddItem(new MenuItem("drawCir", "缁樺埗鎶€鑳借寖鍥磡")).SetValue(true);
                Config.SubMenu("draw").AddItem(new MenuItem("drawCD", "鎶€鑳藉洖澶嶆枃鏈瑋")).SetValue(true);
                Config.SubMenu("draw").AddItem(new MenuItem("drawFull", "缁樺埗瀹屾暣鐨勮繛鍑讳激瀹硘")).SetValue(true);

                Config.AddToMainMenu();
                Drawing.OnDraw += onDraw;
                Drawing.OnEndScene += OnEndScene;

                Game.OnGameUpdate += OnGameUpdate;

                Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
                AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
                Interrupter.OnPosibleToInterrupt += OnPosibleToInterrupt;

            }
            catch
            {
                Game.PrintChat("Oops. Something went wrong with Jayce - Sharp");
            }

        }

        private static void OnEndScene(EventArgs args)
        {
            if (Config.Item("awsPress").GetValue<KeyBind>().Active)
            {
                hpi.drawAwsomee();
            }

            if (Config.Item("drawFull").GetValue<bool>())
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(ene => !ene.IsDead && ene.IsEnemy && ene.IsVisible))
                {
                    hpi.unit = enemy;
                    hpi.drawDmg(Jayce.getJayceFullComoDmg(enemy), Color.Yellow);
                }
        }


        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("gapClose").GetValue<bool>())
                Jayce.knockAway(gapcloser.Sender);
        }
        public static void OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (Config.Item("autoInter").GetValue<bool>() && (int)spell.DangerLevel > 0)
                Jayce.knockAway(unit);
        }

        private static void OnGameUpdate(EventArgs args)
        {
            Jayce.checkForm();
            Jayce.processCDs();
            if (Config.Item("shoot").GetValue<KeyBind>().Active)
            {
                Jayce.shootQE(Game.CursorPos);
            }

            if (!Jayce.E1.IsReady())
                Jayce.castQon = new Vector3(0, 0, 0);

            else if (Jayce.castQon.X != 0)
                Jayce.shootQE(Jayce.castQon);

            if (Config.Item("fullDMG").GetValue<KeyBind>().Active)//fullDMG
            {
                Jayce.activateMura();
                Obj_AI_Hero target = SimpleTs.GetTarget(Jayce.getBestRange(), SimpleTs.DamageType.Physical);
                if (Jayce.lockedTarg == null)
                    Jayce.lockedTarg = target;
                Jayce.doFullDmg(Jayce.lockedTarg);
            }
            else
            {
                Jayce.lockedTarg = null;
            }

            if (Config.Item("injTarget").GetValue<KeyBind>().Active)//fullDMG
            {
                Jayce.activateMura();
                Obj_AI_Hero target = SimpleTs.GetTarget(Jayce.getBestRange(), SimpleTs.DamageType.Physical);
                if (Jayce.lockedTarg == null)
                    Jayce.lockedTarg = target;
                Jayce.doJayceInj(Jayce.lockedTarg);
            }
            else
            {
                Jayce.lockedTarg = null;
            }

            if (Jayce.castEonQ != null && (Jayce.castEonQ.TimeSpellEnd - 2) > Game.Time)
                Jayce.castEonQ = null;

            if (Jayce.orbwalker.ActiveMode.ToString() == "Combo")
            {
                Jayce.activateMura();
                Obj_AI_Hero target = SimpleTs.GetTarget(Jayce.getBestRange(), SimpleTs.DamageType.Physical);
                Jayce.doCombo(target);
            }

            if (Config.Item("killSteal").GetValue<bool>())
                Jayce.doKillSteal();

            if (Jayce.orbwalker.ActiveMode.ToString() == "Mixed")
            {
                Jayce.deActivateMura();
            }

            if (Jayce.orbwalker.ActiveMode.ToString() == "LaneClear")
            {
                Jayce.deActivateMura();
            }
        }

        private static void onDraw(EventArgs args)
        {


            //Draw CD
            if (Config.Item("drawCD").GetValue<bool>())
                Jayce.drawCD();

            if (!Config.Item("drawCir").GetValue<bool>())
                return;
            Utility.DrawCircle(Jayce.Player.Position, !Jayce.isHammer ? 1100 : 600, Color.Red);

            Utility.DrawCircle(Jayce.Player.Position, 1550, Color.Violet);
        }



        public static void OnProcessSpell(Obj_AI_Base obj, GameObjectProcessSpellCastEventArgs arg)
        {
            if (obj.IsMe)
            {

                if (arg.SData.Name == "jayceshockblast")
                {
                    //  Jayce.castEonQ = arg;
                }
                else if (arg.SData.Name == "jayceaccelerationgate")
                {
                    Jayce.castEonQ = null;
                    // Console.WriteLine("Cast dat E on: " + arg.SData.Name);
                }
                Jayce.getCDs(arg);
            }
        }

    }
}
