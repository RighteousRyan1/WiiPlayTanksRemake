using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Properties;
using NativeFileDialogSharp;
using System.IO;
using TanksRebirth.Internals.Common.Utilities;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Net;
using TanksRebirth.Internals.Common;

namespace TanksRebirth.GameContent.RebirthUtils;

public static class DebugManager {
    public static int blockType = 0;
    public static int blockHeight = 1;
    public static int tankToSpawnType;
    public static int tankToSpawnTeam;

    public readonly struct Id {
        public const int General = 0;
        public const int EntityData = 1;
        public const int PlayerData = 2;
        public const int LevelEditDebug = 3;
        public const int Powerups = 4;
        public const int AchievementData = 5;
        // public const int NavData = 6;
    }
    public static string CurDebugLabel {
        get {
            if (DebugLevel < 0 || DebugLevel >= DebuggingNames.Length)
                return $"Unknown - {DebugLevel}";
            else
                return DebuggingNames[DebugLevel];
        }
    }
    public static bool DebuggingEnabled { get; set; }
    public static int DebugLevel { get; set; }

    private static readonly string[] DebuggingNames =
    [
        "General",
        "Entity Data",
        "Player Data",
        "Level Edit Debug",
        "Powerups",
        "Achievement Data"
    ];
    private static readonly PowerupTemplate[] powerups =
     [Powerup.Speed,
         Powerup.ShellHome,
         Powerup.Invisibility];
    public static void DrawDebugString(SpriteBatch sb, object info, Vector2 position, int level = Id.General, float scale = 1f, bool centered = false, Color color = default, bool beginSb = false) {
        if (!DebuggingEnabled || DebugLevel != level)
            return;

        if (beginSb)
            sb.Begin();

        var sizeAdjust = new Vector2(scale * 0.6f * (float)(WindowUtils.WindowWidth / 1920f), scale * 0.6f * (float)(WindowUtils.WindowHeight / 1080f));

        sb.DrawString(TankGame.TextFont, info.ToString(), position, color == default ? Color.White : color, sizeAdjust, 0f, centered ? TankGame.TextFont.MeasureString(info.ToString()) / 2 : default);

        if (beginSb)
            sb.End();
    }
    public static void DrawDebugString(SpriteFontBase font, SpriteBatch sb, object info, Vector2 position, int level = Id.General, float scale = 1f, bool centered = false, Color color = default, bool beginSb = false) {
        if (!DebuggingEnabled || DebugLevel != level)
            return;

        if (beginSb)
            sb.Begin();

        var sizeAdjust = new Vector2(scale * 0.6f * (float)(WindowUtils.WindowWidth / 1920f), scale * 0.6f * (float)(WindowUtils.WindowHeight / 1080f));

        sb.DrawString(font, info.ToString(), position, color == default ? Color.White : color, sizeAdjust, 0f, centered ? TankGame.TextFont.MeasureString(info.ToString()) / 2 : default);

        if (beginSb)
            sb.End();
    }
    public static void DrawDebugTexture(SpriteBatch sb, Texture2D texture, Vector2 position, int level = Id.General, float scale = 1f, Color color = default, bool centered = false, bool beginSb = false) {
        if (!DebuggingEnabled || DebugLevel != level)
            return;

        if (beginSb)
            sb.Begin();

        sb.Draw(texture, position, null, color == default ? Color.White : color, 0f, centered ? texture.Size() / 2 : default, scale, default, 0f);

        if (beginSb)
            sb.End();
    }

    public static UITextButton ClearTracks;
    public static UITextButton ClearChecks;

    public static UITextButton SetupMissionAgain;

    public static UITextButton MovePURight;
    public static UITextButton MovePULeft;

    public static UITextButton Display;

    public static UITextInput MissionName;
    public static UITextInput CampaignName;
    public static UITextButton LoadMission;
    public static UITextButton SaveMission;

    public static UITextButton LoadCampaign;

    private static int mode;

    public static void InitDebugUI() {
        MissionName = new(TankGame.TextFont, Color.White, 0.75f, 20) {
            DefaultString = "Mission Name",
            IsVisible = false
        };
        MissionName.SetDimensions(20, 60, 230, 50);
        CampaignName = new(TankGame.TextFont, Color.White, 0.75f, 20) {
            DefaultString = "Campaign Name",
            IsVisible = false
        };
        CampaignName.SetDimensions(20, 120, 230, 50);

        SaveMission = new("Save", TankGame.TextFont, Color.White, 0.5f);
        SaveMission.OnLeftClick = (l) => {
            if (MissionName.IsEmpty()) {
                ChatSystem.SendMessage("Invalid name for mission.", Color.Red);
                return;
            }
            Mission.Save(MissionName.GetRealText(), CampaignName.IsEmpty() ? null : CampaignName.GetRealText());

            ChatSystem.SendMessage(CampaignName.IsEmpty() ? $"Saved mission '{MissionName.GetRealText()}'." : $"Saved mission '{MissionName.GetRealText()}' to Campaign folder '{CampaignName.GetRealText()}'.", Color.White);
        };
        SaveMission.IsVisible = false;
        SaveMission.SetDimensions(20, 180, 105, 50);

        LoadMission = new("Load", TankGame.TextFont, Color.White, 0.5f);
        LoadMission.OnLeftClick = (l) => {
            if (TankGame.IsWindows && MissionName.IsEmpty()) {
                var res = Dialog.FileOpen("mission", TankGame.SaveDirectory);
                if (res.Path != null && res.IsOk) {
                    try {
                        GameProperties.LoadedCampaign.LoadMission(Mission.Load(res.Path, null));
                        GameProperties.LoadedCampaign.SetupLoadedMission(true);

                        ChatSystem.SendMessage($"Loaded mission '{Path.GetFileNameWithoutExtension(res.Path)}'.", Color.White);
                    } catch {
                        ChatSystem.SendMessage("Failed to load mission.", Color.Red);
                    }
                }
                return;
            }

            GameProperties.LoadedCampaign.LoadMission(Mission.Load(MissionName.GetRealText(), CampaignName.IsEmpty() ? null : CampaignName.GetRealText()));
            GameProperties.LoadedCampaign.SetupLoadedMission(true);
        };
        LoadMission.IsVisible = false;
        LoadMission.SetDimensions(145, 180, 105, 50);

        LoadCampaign = new("Load Campaign", TankGame.TextFont, Color.White, 0.75f) {
            OnLeftClick = (l) => {
                if (MissionName.IsEmpty()) {
                    ChatSystem.SendMessage("Invalid name for campaign.", Color.Red);
                    return;
                }
                GameProperties.LoadedCampaign = Campaign.LoadFromFolder(CampaignName.GetRealText(), true);
                GameProperties.LoadedCampaign.SetupLoadedMission(true);
            },
            IsVisible = false
        };
        LoadCampaign.SetDimensions(20, 240, 230, 50);

        ClearTracks = new("Clear Tracks", TankGame.TextFont, Color.LightBlue, 0.5f);
        ClearTracks.SetDimensions(250, 25, 100, 50);
        ClearTracks.IsVisible = false;

        ClearTracks.OnLeftClick = (a) => SceneManager.ClearTankTracks();

        ClearChecks = new("Clear Checks", TankGame.TextFont, Color.LightBlue, 0.5f);
        ClearChecks.SetDimensions(250, 95, 100, 50);
        ClearChecks.IsVisible = false;

        ClearChecks.OnLeftClick = (a) => SceneManager.ClearTankDeathmarks();

        SetupMissionAgain = new("Restart\nMission", TankGame.TextFont, Color.LightBlue, 0.5f);
        SetupMissionAgain.SetDimensions(250, 165, 100, 50);
        SetupMissionAgain.IsVisible = false;

        SetupMissionAgain.OnLeftClick = (obj) => IntermissionHandler.BeginIntroSequence();

        MovePULeft = new("<", TankGame.TextFont, Color.LightBlue, 0.5f);
        MovePULeft.SetDimensions(WindowUtils.WindowWidth / 2 - 100, 25, 50, 50);
        MovePULeft.IsVisible = false;

        MovePURight = new(">", TankGame.TextFont, Color.LightBlue, 0.5f);
        MovePURight.SetDimensions(WindowUtils.WindowWidth / 2 + 100, 25, 50, 50);
        MovePURight.IsVisible = false;

        Display = new(powerups[mode].Name, TankGame.TextFont, Color.LightBlue, 0.5f);
        Display.SetDimensions(WindowUtils.WindowWidth / 2 - 35, 25, 125, 50);
        Display.IsVisible = false;

        MovePULeft.OnLeftClick = (obj) => {
            if (mode < powerups.Length - 1)
                mode++;
            Display.Text = powerups[mode].Name;
        };
        MovePURight.OnLeftClick = (obj) => {
            if (mode > 0)
                mode--;
            Display.Text = powerups[mode].Name;
        };
    }
    public static void UpdateDebug() {
        if (!MainMenu.Active) {
            ClearTracks.IsVisible = DebuggingEnabled && DebugLevel == 0;
            ClearChecks.IsVisible = DebuggingEnabled && DebugLevel == 0;
            SetupMissionAgain.IsVisible = DebuggingEnabled && DebugLevel == 0;
            MovePULeft.IsVisible = DebuggingEnabled && DebugLevel == 4;
            MovePURight.IsVisible = DebuggingEnabled && DebugLevel == 4;
            Display.IsVisible = DebuggingEnabled && DebugLevel == 4;
            MissionName.IsVisible = DebuggingEnabled && DebugLevel == 3;
            LoadMission.IsVisible = DebuggingEnabled && DebugLevel == 3;
            SaveMission.IsVisible = DebuggingEnabled && DebugLevel == 3;
            LoadCampaign.IsVisible = DebuggingEnabled && DebugLevel == 3;
            CampaignName.IsVisible = DebuggingEnabled && DebugLevel == 3;
        }
        if (InputUtils.KeyJustPressed(Keys.Insert))
            DebuggingEnabled = !DebuggingEnabled;

        if (InputUtils.KeyJustPressed(Keys.Multiply))
            DebugLevel++;
        if (InputUtils.KeyJustPressed(Keys.Divide))
            DebugLevel--;
        if (!MainMenu.Active) {
            if (InputUtils.KeyJustPressed(Keys.Z))
                blockType--;
            if (InputUtils.KeyJustPressed(Keys.X))
                blockType++;
            if (DebugManager.DebuggingEnabled) {
                if (InputUtils.KeyJustPressed(Keys.NumPad7))
                    tankToSpawnType--;
                if (InputUtils.KeyJustPressed(Keys.NumPad9))
                    tankToSpawnType++;

                if (InputUtils.KeyJustPressed(Keys.NumPad1))
                    tankToSpawnTeam--;
                if (InputUtils.KeyJustPressed(Keys.NumPad3))
                    tankToSpawnTeam++;

                if (InputUtils.KeyJustPressed(Keys.OemPeriod))
                    blockHeight++;
                if (InputUtils.KeyJustPressed(Keys.OemComma))
                    blockHeight--;


                if (InputUtils.KeyJustPressed(Keys.PageUp))
                    SpawnTankPlethorae(true);
                if (InputUtils.KeyJustPressed(Keys.PageDown))
                    SpawnMe(/*PlayerID.Blue*/GameHandler.GameRand.Next(PlayerID.Blue, PlayerID.YellowPlr + 1), tankToSpawnTeam);
                if (InputUtils.KeyJustPressed(Keys.Home))
                    SpawnTankAt(!TankGame.OverheadView ? MatrixUtils.GetWorldPosition(MouseUtils.MousePosition) : PlacementSquare.CurrentlyHovered.Position, tankToSpawnType, tankToSpawnTeam);

                if (InputUtils.KeyJustPressed(Keys.OemSemicolon))
                    new Mine(null, MatrixUtils.GetWorldPosition(MouseUtils.MousePosition).FlattenZ(), 400);
                if (InputUtils.KeyJustPressed(Keys.OemQuotes))
                    new Shell(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition).FlattenZ(), Vector2.Zero, ShellID.Standard, null!, 0);
                if (InputUtils.KeyJustPressed(Keys.End))
                    SpawnCrateAtMouse();

                if (InputUtils.KeyJustPressed(Keys.I) && DebugManager.DebugLevel == 4)
                    new Powerup(powerups[mode]) { Position = MatrixUtils.GetWorldPosition(MouseUtils.MousePosition) + new Vector3(0, 10, 0) };
            }
        }
        blockHeight = MathHelper.Clamp(blockHeight, 1, 7);
        blockType = MathHelper.Clamp(blockType, 0, 3);
    }

    public static void SpawnCrateAtMouse() {
        var pos = MatrixUtils.GetWorldPosition(MouseUtils.MousePosition);

        var drop = Crate.SpawnCrate(new(pos.X, 200, pos.Z), 2f);
        drop.scale = 1.25f;
        drop.TankToSpawn = new TankTemplate() {
            AiTier = AITank.PickRandomTier(),
            Team = TeamID.NoTeam
        };
    }

    public static AITank SpawnTank(int tier, int team) {
        var rot = GeometryUtils.GetPiRandom();

        var t = new AITank(tier);
        t.TankRotation = rot;
        t.TurretRotation = rot;
        t.Team = team;
        t.Dead = false;
        var pos = new BlockMapPosition(GameHandler.GameRand.Next(0, 27), GameHandler.GameRand.Next(0, 20));
        t.Body.Position = pos;
        t.Position = pos;

        return t;
    }
    public static AITank SpawnTankAt(Vector3 position, int tier, int team) {
        var rot = 0f;

        var x = new AITank(tier);
        x.TargetTankRotation = rot;
        x.TankRotation = rot;
        x.TurretRotation = rot;

        x.Team = team;
        x.Dead = false;
        x.Body.Position = position.FlattenZ() / Tank.UNITS_PER_METER;
        x.Position = position.FlattenZ();
        return x;
    }
    public static void SpawnTankPlethorae(bool useCurTank = false) {
        for (int i = 0; i < 5; i++) {
            var random = new BlockMapPosition(GameHandler.GameRand.Next(0, 23), GameHandler.GameRand.Next(0, 18));
            var rot = GeometryUtils.GetPiRandom();
            var t = new AITank(useCurTank ? tankToSpawnType : AITank.PickRandomTier());
            t.TankRotation = rot;
            t.TurretRotation = rot;
            t.Dead = false;
            t.Team = useCurTank ? tankToSpawnTeam : TeamID.NoTeam;
            t.Body.Position = random;
            t.Position = random;
        }
    }
    public static PlayerTank SpawnMe(int playerType, int team, Vector3 posOverride = default) {
        var pos = LevelEditor.Active ? PlacementSquare.CurrentlyHovered.Position : MatrixUtils.GetWorldPosition(MouseUtils.MousePosition);

        if (posOverride != default)
            pos = posOverride;
        var myTank = new PlayerTank(playerType) {
            Team = team,
            Dead = false
        };
        myTank.Body.Position = pos.FlattenZ() / Tank.UNITS_PER_METER;
        myTank.Position = pos.FlattenZ();

        if (Client.IsConnected())
            Client.RequestPlayerTankSpawn(myTank);

        return myTank;
    }

    public static void SpawnTankInCrate(int tierOverride = default, int teamOverride = default, bool createEvenDrop = false) {
        var random = new BlockMapPosition(GameHandler.GameRand.Next(0, 26), GameHandler.GameRand.Next(0, 20));

        var drop = Crate.SpawnCrate(new(BlockMapPosition.Convert3D(random).X, 500 + (createEvenDrop ? 0 : GameHandler.GameRand.Next(-300, 301)), BlockMapPosition.Convert3D(random).Z), 2f);
        drop.scale = 1.25f;
        drop.TankToSpawn = new TankTemplate() {
            AiTier = tierOverride == default ? AITank.PickRandomTier() : tierOverride,
            Team = teamOverride == default ? GameHandler.GameRand.Next(TeamID.NoTeam, TeamID.Collection.Count) : teamOverride
        };
    }
}