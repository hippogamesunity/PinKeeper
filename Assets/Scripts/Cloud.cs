using Assets.Scripts.Common;
using Assets.Scripts.Views;
using GooglePlayGames;
using GooglePlayGames.BasicApi.SavedGame;
using System;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
    public class Cloud : Script
    {
        public UILabel SyncInfo;

        private readonly GamesServicesClient _cloud = new GamesServicesClient();
        private bool _storageChanged;
        private string _exception;
        private const byte NullByte = 255;
        private const string SaveName = "PinKeeper2";
        private bool _reset;

        public void Start()
        {
            _cloud.Exception += Exception;
            _cloud.GameLoaded += GameLoaded;
            _cloud.GameSaved += GameSaved;
        }

        public void Sync()
        {
            if (!Profile.Instance.Premium || _cloud.Busy) return;

            WriteSyncMessage("%Connecting%");
            Initialize();
            _cloud.Load(SaveName);
        }

        public void Reset()
        {
            if (!Profile.Instance.Premium || _cloud.Busy) return;

            WriteSyncMessage("%Connecting%");
            Initialize();
            _reset = true;
            _cloud.Load(SaveName);
        }

        private void Initialize()
        {
            _storageChanged = false;
            _exception = null;
            _reset = false;
        }

        private void Exception(GamesServicesClient.ErrorCodes errorCode, string errorMessage)
        {
            switch (errorCode)
            {
                case GamesServicesClient.ErrorCodes.AuthenticationError:
                    WriteSyncMessage("%AuthenticationError%");
                    break;
                case GamesServicesClient.ErrorCodes.Exception:
                    _exception = errorMessage;
                    break;
            }
        }

        private void GameSaved(bool success)
        {
            if (!success)
            {
                WriteSyncMessage("%SaveFailed%");
                return;
            }
            
            if (_exception != null)
            {
                WriteSyncMessage(_exception);
                return;
            }

            Profile.Instance.SaveSyncTime();

            if (_storageChanged)
            {
                GetComponent<PatternLock>().Open(TweenDirection.Right);
            }

            WriteSyncMessage("%Synced%");
        }

        private void GameLoaded(bool success, ISavedGameMetadata meta, byte[] data)
        {
            if (!success)
            {
                WriteSyncMessage("%LoadFailed%");
                return;
            }

            if (_exception != null)
            {
                WriteSyncMessage(_exception);
                return;
            }

            Profile.Instance.CreateToken(new ProtectedValue(PlayGamesPlatform.Instance.GetUserId()), PatternLock.Pattern.Copy());

            if (_reset)
            {
                _cloud.Save(meta, new[] { NullByte });
                return;
            }

            if (IsEmpty(data))
            {
                if (Profile.Instance.CountCards() == 0)
                {
                    WriteSyncMessage("%NothingToSync%");
                }
                else
                {
                    _cloud.Save(meta, Profile.Instance.Encrypt());
                }
            }
            else
            {
                try
                {
                    _storageChanged = Profile.Instance.Merge(Encoding.UTF8.GetString(data));

                    if (Profile.Instance.ReadyForSave)
                    {
                        _cloud.Save(meta, Profile.Instance.Encrypt());
                    }
                    else
                    {
                        GetComponent<PatternLock>().Open(TweenDirection.Right);
                        WriteSyncMessage("%Synced%");
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    WriteSyncMessage(e.Message);
                }
            }
        }

        private static bool IsEmpty(byte[] data)
        {
            return data == null || (data.Length == 1 && data[0] == NullByte) || string.IsNullOrEmpty(Encoding.UTF8.GetString(data));
        }

        private void WriteSyncMessage(string message)
        {
            SyncInfo.SetLocalizedText(message);
        }
    }
}