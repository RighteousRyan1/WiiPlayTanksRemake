﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;
using FontStashSharp;
using TanksRebirth.GameContent.Systems.Coordinates;
using NativeFileDialogSharp;
using Microsoft.Xna.Framework.Input;

namespace TanksRebirth.GameContent.UI
{
    public static class CampaignCompleteUI
    {
        public static bool IsViewingResults;
        public static Rectangle ParseGradeRect(Grade grade)
        {
            // 128 being the width of each section.
            int col = (int)grade / 3;
            int row = (int)grade % 3;

            return new(col * 128, row * 128, 128, 128);
        }

        private static TimeSpan _delayPerTank = TimeSpan.FromMilliseconds(500);

        public static Dictionary<TankTier, int> KillsPerType = new();

        public static Grade Grade;

        private static float _gradeAlpha;
        private static float _gradeScale;
        private static float _gradeScaleDefault = 3f;
        private static float _gradeFadeSpeed = 0.1f;
        private static float _gradeTargetScale = 1f;

        private static float _panelAlpha = 0f;
        private static float _panelAlphaMax = 1f;
        private static float _panelFadeSpeed = 0.03f;

        public static bool _skip;
        // TODO: tomorrow do this stuff
        // music loop, depending on end context
        // tank grafiks
        private static bool[] _tierDisplays;
        private static bool _shouldShowGrade;
        private static float[] _tierAlphas;
        private static float[] _tierScales;
        private static float _defaultSize = 2.5f;
        private static float _appearSpeed = 0.1f;
        private static float _tankSize = 1.5f;
        private static float _spacePerTank = 30f;
        private static float _tnkDrawYOff = 200f;
        private static Color _textColor = Color.DarkGray;

        public static int TanksPerColumn = 18;

        private static MissionEndContext _lastContext;

        public static Dictionary<MissionEndContext, (OggAudio, TimeSpan)> FanfaresAndDurations = new()
        {
            [MissionEndContext.CampaignCompleteMinor] = (new OggAudio("Content/Assets/fanfares/mission_complete_M20"), TimeSpan.FromMilliseconds(3400)), // 4327
            [MissionEndContext.CampaignCompleteMajor] = (new OggAudio("Content/Assets/fanfares/mission_complete_M100"), TimeSpan.FromMilliseconds(2600)), // 3350
            [MissionEndContext.GameOver] = (new OggAudio("Content/Assets/fanfares/gameover_playerdeath"), TimeSpan.FromMilliseconds(1400)) // 1883
        };
        public static OggMusic ResultsFanfare = new("Results Fanfare", "Content/Assets/fanfares/endresults", 0.6f);

        /// <summary>Perform a multithreaded operation that will display tanks killed and their kill counts for the player.</summary>
        public static void PerformSequence(MissionEndContext context)
        {
            IsViewingResults = true;

            _lastContext = context;

            _shouldShowGrade = false;
            _gradeScale = _gradeScaleDefault;
            _gradeAlpha = 0f;
            _soundPlayed = false;

            if (context != MissionEndContext.GameOver)
                TankGame.GameData.CampaignsCompleted++;

            // TierDisplays?.Clear();
            // then reinitialize with the proper tank-to-kill values.
            // *insert code here*

            // finish this tomorrow i am very tired murder me.
            SetStats(GameProperties.LoadedCampaign, PlayerTank.PlayerStatistics, PlayerTank.TankKills);
            Grade = FormulateGradeLevel();

            _skip = false;

            Task.Run(() => {
                while (_panelAlpha < 1f)
                    Thread.Sleep(TankGame.LastGameTime.ElapsedGameTime);
                if (!ResultsFanfare.IsPlaying()) {
                    ResultsFanfare.SetVolume(1f);
                    ResultsFanfare.Play();
                }
                // sleep the thread for the duration of the fanfare.
                Vector2 basePos = new(GameUtils.WindowWidth / 2, GameUtils.WindowHeight * 0.2f);
                for (int i = 0; i < KillsPerType.Count; i++)
                {
                    ModifyTracks();

                    var killData = KillsPerType.ElementAt(i);

                    _tierDisplays[i] = true;

                    //ChatSystem.SendMessage($"{killData.Key}:{killData.Value}", Color.White);

                    if (!_skip)
                    {
                        if (i % 4 == 3) // i think this is the value
                            SoundPlayer.PlaySoundInstance("Assets/sounds/results/whistle_double", SoundContext.Effect, 1f);
                        else
                            SoundPlayer.PlaySoundInstance("Assets/sounds/results/whistle_singular", SoundContext.Effect, 1f);

                        Thread.Sleep(_delayPerTank);

                        //ChatSystem.SendMessage("sleep", Color.White);
                    }
                }

                //ChatSystem.SendMessage("whistle", Color.White);
                SoundPlayer.PlaySoundInstance("Assets/sounds/results/whistle_full", SoundContext.Effect, 1f);

                _skip = true;
                _shouldShowGrade = true;

                while (!Input.KeyJustPressed(Keys.Enter)) {
                    ModifyTracks();
                    Thread.Sleep(TankGame.LastGameTime.ElapsedGameTime);
                }

                _panelAlpha = 0f;
                _gradeAlpha = 0f;
                IsViewingResults = false;
                if (!ResultsFanfare.IsStopped())
                    ResultsFanfare.Stop();
                MainMenu.Open();
            });
            // changing bools in other threads = safe
            // - ryan
        }

        private static void ModifyTracks() {
            try {
                ResultsFanfare.SetVolume(TankGame.Instance.IsActive ? TankGame.Settings.MusicVolume : 0f);
            } catch { }
        }

        private static void ResetThings()
        {
            _appearSpeed = 0.1f;
            _defaultSize = 4f;
            _tankSize = 1.5f;
            _spacePerTank = 30f;
            _textColor = Color.DarkGray;
            _tnkDrawYOff = 200f;
            for (int j = 0; j < _tierDisplays.Length; j++)
            {
                _tierScales[j] = _defaultSize;
                _tierAlphas[j] = 0;
            }
        }
        private static bool _soundPlayed;
        public static void Render()
        {
            if (Input.KeyJustPressed(Keys.Enter))
                _skip = true;

            _panelAlpha += _panelFadeSpeed;
            if (_panelAlpha > _panelAlphaMax)
                _panelAlpha = _panelAlphaMax;
                
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Vector2(GameUtils.WindowWidth / 3, 0), null, Color.Beige * _panelAlpha, 0f, Vector2.Zero, new Vector2(GameUtils.WindowWidth / 3, GameUtils.WindowHeight), default, 0f);
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Vector2(GameUtils.WindowWidth / 3, (_tnkDrawYOff - 50f).ToResolutionY()), null, Color.Gold * _panelAlpha, 0f, Vector2.Zero, new Vector2(GameUtils.WindowWidth / 3, 10f.ToResolutionY()), default, 0f);

            if (_shouldShowGrade) {
                _gradeAlpha += _gradeFadeSpeed / 2;
                _gradeScale -= _gradeFadeSpeed;
                if (_gradeAlpha > 1f)
                    _gradeAlpha = 1f;
                if (_gradeScale <= _gradeTargetScale)
                {
                    _gradeScale = _gradeTargetScale;

                    if (!_soundPlayed)
                    {
                        SoundPlayer.PlaySoundInstance("Assets/sounds/results/punch", SoundContext.Effect);
                        _soundPlayed = true;
                    }
                }
                var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/grades");
                TankGame.SpriteRenderer.Draw(tex, new Vector2(GameUtils.WindowWidth / 3 * 2, 250.ToResolutionY()), ParseGradeRect(Grade), Color.White * _gradeAlpha, 0f, new Vector2(64, 64), new Vector2(_gradeScale).ToResolution(), default, 0f);
            }

            if (DebugUtils.DebuggingEnabled) {
                if (Input.AreKeysJustPressed(Keys.O, Keys.P)) {
                    ResetThings();

                    PerformSequence(_lastContext);
                }
                else if (Input.KeyJustPressed(Keys.P))
                    ResetThings();
            }

            #region Tank Graphics
            float offY = 0;
            int xAdjust = 200;
            int xAdjustCount = 0;
            for (int i = 0; i < KillsPerType.Count; i++)
            {
                var display = KillsPerType.ElementAt(i);

                if (_tierDisplays[i])
                {
                    #region Handle Appearance

                    _tierScales[i] -= _appearSpeed;
                    _tierAlphas[i] += _appearSpeed;

                    if (_tierScales[i] < _tankSize)
                        _tierScales[i] = _tankSize;

                    if (_tierAlphas[i] > 1f)
                        _tierAlphas[i] = 1f;
                    #endregion

                    xAdjustCount = (int)Math.Floor((float)i / TanksPerColumn);

                    var texture = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/tank2d");

                    // draw the tank graphic
                    // i love weird math.
                    // GameUtils.ToResolution is kinda wack here, so i'll exclude it.
                    IntermissionSystem.DrawShadowedTexture(texture,
                        new Vector2(
                            GameUtils.WindowWidth / 3 + GameUtils.WindowWidth * 0.025f + xAdjust * xAdjustCount * ((float)GameUtils.WindowWidth / 1920),
                            _tnkDrawYOff.ToResolutionY() + offY - (xAdjustCount * _spacePerTank * TanksPerColumn * _tankSize * ((float)GameUtils.WindowHeight / 1080))),
                        Vector2.One, AITank.TankDestructionColors[display.Key],
                        new Vector2(_tierScales[i]) * (GameUtils.WindowBounds / new Vector2(1920, 1080)),
                        _tierAlphas[i], texture.Size() / 2, shadowDistScale: 0.4f);
                    // draw the kill count text
                    IntermissionSystem.DrawShadowedString(TankGame.TextFont,
                        new Vector2(
                            GameUtils.WindowWidth / 3 + GameUtils.WindowWidth * 0.025f + (xAdjust / 2).ToResolutionX() + xAdjust * xAdjustCount * ((float)GameUtils.WindowWidth / 1920),
                            _tnkDrawYOff.ToResolutionY() - 2f.ToResolutionY() + offY - (xAdjustCount * _spacePerTank * TanksPerColumn * _tankSize * ((float)GameUtils.WindowHeight / 1080))),
                        Vector2.One, $"{display.Value}", _textColor, new Vector2(_tierScales[i]) * (GameUtils.WindowBounds / new Vector2(1920, 1080)), _tierAlphas[i], TankGame.TextFont.MeasureString($"{display.Value}") / 2, 
                        shadowDistScale: 0.4f);

                    offY += _spacePerTank * _tankSize * ((float)GameUtils.WindowHeight / 1080);
                }
            }
            #endregion
            var txt = _skip ? "Press 'Enter' to exit." : "Press 'Enter' to skip.";
            var measure = TankGame.TextFont.MeasureString(txt);
            IntermissionSystem.DrawShadowedString(TankGame.TextFont, new Vector2(GameUtils.WindowWidth / 2, GameUtils.WindowHeight - 8), Vector2.One, txt, Color.Black, Vector2.One.ToResolution(), 1f, new Vector2(measure.X / 2, measure.Y), 0.4f);

            var txt1 = "Campaign Results";
            var measure1 = TankGame.TextFontLarge.MeasureString(txt1);
            IntermissionSystem.DrawShadowedString(TankGame.TextFontLarge, new Vector2(GameUtils.WindowWidth / 2, 8), Vector2.One, txt1, Color.DeepSkyBlue, new Vector2(0.5f).ToResolution(), _panelAlpha, new Vector2(measure1.X / 2, 0));
        }
        // TODO: probably support multiple players L + ratio me
        public static void SetStats(Campaign campaign, PlayerTank.DeterministicPlayerStats stats, Dictionary<TankTier, int> killCounts, bool orderByTier = true)
        {
            // set everything properly...
            KillsPerType = killCounts;
            _tierAlphas = new float[killCounts.Count];
            _tierScales = new float[killCounts.Count];
            _tierDisplays = new bool[killCounts.Count];

            Array.Fill(_tierScales, _defaultSize);

            ShellsFired = stats.ShellsShotThisCampaign;
            ShellHits = stats.ShellHitsThisCampaign;

            MinesLaid = stats.MinesLaidThisCampaign;
            MineHits = stats.MineHitsThisCampaign;

            SuicideCount = stats.SuicidesThisCampaign;

            LivesRemaining = PlayerTank.Lives;

            // then, we determine the number of possible lives by checking how many extra life missions
            // there were this campaign, along with adding the starting life count.
            TotalPossibleLives = PlayerTank.StartingLives + campaign.Properties.ExtraLivesMissions.Length;

            if (orderByTier)
                KillsPerType = KillsPerType.OrderBy(tier => tier.Key).ToDictionary(x => x.Key, y => y.Value);
        }

        public static Grade FormulateGradeLevel()
        {
            var grade = Grade.APlus;

            float shellRatio = (float)ShellHits / ShellsFired;
            float mineRatio = (float)MineHits / MinesLaid;
            float lifeRatio = (float)LivesRemaining / TotalPossibleLives;

            if (float.IsNaN(shellRatio))
                shellRatio = 0f;
            if (float.IsNaN(mineRatio))
                mineRatio = 0f;

            // (min, max]
            bool isBetween(float input, float min, float max) => input >= min && input < max;
            // redundant code but whatever i love men
            if (shellRatio > 0.8f)
                grade += 0;
            else if (isBetween(shellRatio, 0.6f, 0.8f))
                grade += 1;
            else if (isBetween(shellRatio, 0.4f, 0.6f))
                grade += 2;
            else if (isBetween(shellRatio, 0.2f, 0.4f))
                grade += 3;
            else if (shellRatio < 0.2f)
                grade += 4;

            if (mineRatio >= 0.25f)
                grade += 0;
            else if (mineRatio < 0.25f)
                grade += 1;

            // check >= just incase something goofy happens.
            if (lifeRatio >= 1f)
                grade += 0;
            else if (isBetween(lifeRatio, 0.75f, 1f))
                grade += 1;
            else if (isBetween(lifeRatio, 0.5f, 0.75f))
                grade += 2;
            else if (isBetween(lifeRatio, 0.25f, 0.5f))
                grade += 3;
            else if (isBetween(lifeRatio, 0f, 0.25f))
                grade += 4;

            grade += SuicideCount;

            if (grade > Grade.FMinus)
                grade = Grade.FMinus;

            return grade;
        }

        // overall stats throughout the campaign
        // these will factor into the grade level

        public static int ShellsFired;
        public static int ShellHits;

        // considering not using this...
        public static int MinesLaid;
        public static int MineHits;

        public static int TotalPossibleLives;
        public static int LivesRemaining;

        public static int SuicideCount;
    }
}