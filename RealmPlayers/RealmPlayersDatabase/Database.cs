﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VF_RealmPlayersDatabase
{
    public class Database
    {
        public static WowRealm[] ALL_REALMS = new WowRealm[] { 
            WowRealm.Kronos, 
            WowRealm.NostalGeek, 
            WowRealm.Nostalrius, 
            WowRealm.NostalriusPVE, 
            WowRealm.Emerald_Dream, 
            WowRealm.Warsong, 
            WowRealm.Al_Akir, 
            WowRealm.Valkyrie, 
            WowRealm.VanillaGaming, 
            WowRealm.Nefarian, 
            WowRealm.Rebirth, 
            WowRealm.Archangel };

        Dictionary<WowRealm, RealmDatabase> m_Realms = new Dictionary<WowRealm, RealmDatabase>();

        public Database(string _RootPath, DateTime? _HistoryEarliestTime = null, WowRealm[] _Realms = null)
        {
            WowRealm[] loadRealms = _Realms;
            if (_Realms == null)
                loadRealms = ALL_REALMS;
            foreach (WowRealm loadRealm in loadRealms)
            {
                try
                {
                    LoadRealmDatabase(_RootPath, loadRealm, _HistoryEarliestTime);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
        }
        public bool IsDBFilesUpdated(string _RootPath, object _SynchronizationLockObject)
        {
            if(_SynchronizationLockObject == null)
                _SynchronizationLockObject = new object();
            
            lock(_SynchronizationLockObject)
            {
                foreach (var realm in m_Realms)
                {
                    if (realm.Value.IsDBFileUpdated(_RootPath + realm.Key.ToString() + "\\") == true)
                        return true;
                }
            }
            return false;
        }
        public bool ReloadAllRealmDBs(string _RootPath, bool _PurgeRealmDB = true, object _SynchronizationLockObject = null, DateTime? _HistoryEarliestTime = null)
        {
            bool reloadedAnyRealm = false;
            WowRealm[] realms = m_Realms.Keys.ToArray<WowRealm>();
            foreach(WowRealm realm in realms)
            {
                reloadedAnyRealm = ReloadRealmDB(realm, _RootPath, _PurgeRealmDB, _SynchronizationLockObject, _HistoryEarliestTime) || reloadedAnyRealm;
            }
            return reloadedAnyRealm;
        }
        public bool ReloadRealmDB(WowRealm _Realm, string _RootPath, bool _PurgeRealmDB = true, object _SynchronizationLockObject = null, DateTime? _HistoryEarliestTime = null)
        {
            if (_SynchronizationLockObject == null)
                _SynchronizationLockObject = new object();

            try
            {
                if(m_Realms.ContainsKey(_Realm) == true)
                {
                    RealmDatabase oldRealm;
                    lock (_SynchronizationLockObject)
                    {
                        oldRealm = m_Realms[_Realm];
                    }
                    if(oldRealm.IsDBFileUpdated(_RootPath + _Realm.ToString() + "\\") == true)
                    {
                        var reloadedRealm = new RealmDatabase(_Realm);
                        reloadedRealm.LoadDatabase(_RootPath + _Realm.ToString() + "\\", _HistoryEarliestTime);
                        if (_PurgeRealmDB == true)
                        {
                            var purgeTask = new System.Threading.Tasks.Task(() =>
                            {
                                reloadedRealm.RemoveUnknowns();
                                reloadedRealm.RemoveGMs();
                                GC.Collect();
                            });
                            purgeTask.Start();
                            purgeTask.Wait(300 * 1000);//300 sekunder(5 minuter)
                        }
                        reloadedRealm.WaitForLoad(RealmDatabase.LoadStatus.EverythingLoaded);
                        if (reloadedRealm.IsLoadComplete() == false)
                        {
                            Logger.ConsoleWriteLine("Failed to reload database " + _Realm + ", it took too long", ConsoleColor.Red);
                            return false;
                        }
                        lock (_SynchronizationLockObject)
                        {
                            m_Realms[_Realm] = reloadedRealm;
                        }
                        GC.Collect();
                        return true;
                    }
                }
                else
                {
                    Logger.ConsoleWriteLine("Realm was not loaded when requesting reload, this is unoptimal solution due to unnecessary locking for a long time!", ConsoleColor.Red);
                    try
                    {
                        lock (_SynchronizationLockObject)
                        {
                            LoadRealmDatabase(_RootPath, _Realm, _HistoryEarliestTime);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return false;
        }
        public void PurgeRealmDBs(bool _PurgeUnknowns, bool _PurgeGMs, bool _WaitForComplete = true)
        {
            List<System.Threading.Tasks.Task> purgeTasks = new List<System.Threading.Tasks.Task>();
            foreach (var realmDB in m_Realms)
            {
                try
                {
                    var purgeTask = new System.Threading.Tasks.Task(() =>
                    {
                        if (_PurgeUnknowns == true)
                            realmDB.Value.RemoveUnknowns();
                        if (_PurgeGMs == true)
                            realmDB.Value.RemoveGMs();
                        GC.Collect();
                    });
                    purgeTask.Start();
                    purgeTasks.Add(purgeTask);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
            if (_WaitForComplete == true)
            {
                foreach (var purgeTask in purgeTasks)
                {
                    purgeTask.Wait(1200 * 1000);//1200 sekunder(20 minuter)
                }
            }
        }

        public Dictionary<WowRealm, RealmDatabase> GetRealms()
        {
            return m_Realms;
        }
        public void AddContribution(RPPContribution _Contribution)
        {
            int loggedExceptions = 0;
            try
            {
                SavedVariablesParser.Document doc = new SavedVariablesParser.Document(_Contribution.GetFilename());
                var xmlDoc = doc.ConvertToXMLDocument();

                WowVersionEnum wowVersion = WowVersionEnum.Unknown;
                try
                {
                    string addonVersion = XMLUtility.GetChildValue(xmlDoc.DocumentElement, "VF_RealmPlayersVersion", "0.0");
                    if (addonVersion.Split('.').Length == 2) //VF_RealmPlayers
                    {
                        if (Utility.ParseDouble(addonVersion) <= 1.58)
                            return;
                        wowVersion = WowVersionEnum.Vanilla;
                    }
                    else //VF_RealmPlayersTBC
                    {
                        wowVersion = WowVersionEnum.TBC;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
                var dataNode = XMLUtility.GetChild(xmlDoc.DocumentElement, "VF_RealmPlayersData");
                foreach (System.Xml.XmlNode playerNode in dataNode.ChildNodes)
                {
                    try
                    {
                        if (XMLUtility.GetChild(playerNode, "PlayerData") != null)
                        {
                            string realmStr = PlayerData.DataParser.ParseRealm(playerNode);
                            WowRealm realm = StaticValues.ConvertRealm(realmStr);
                            if(realm == WowRealm.Archangel || wowVersion == WowVersionEnum.TBC)
                            {
                                if(realm != WowRealm.Archangel || wowVersion != WowVersionEnum.TBC)
                                {
                                    Logger.ConsoleWriteLine("RealmPlayers WoWversion guess was wrong!!!", ConsoleColor.Red);
                                }
                            }

                            if (realm == WowRealm.Unknown || m_Realms.ContainsKey(realm) == false)
                            {
                                Logger.ConsoleWriteLine("RealmStr: \"" + realmStr + "\" was not recognized as a realm");
                            }
                            else
                            {
                                RealmDatabase realmDB = m_Realms[realm];
                                realmDB.UpdatePlayer(playerNode, _Contribution.GetContributor());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (loggedExceptions < 5)
                            Logger.LogException(ex);
                        ++loggedExceptions;
                    }
                }
                Logger.ConsoleWriteLine(_Contribution.GetContributor().GetFilename() + " just updated database successfully!");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
        public void PurgeGearContribution(WowRealm _Realm, string _Character, UploadID _UploadID)
        {
            RealmDatabase realmDB = m_Realms[_Realm];
            realmDB.PurgeGearContribution(_Character, _UploadID);
        }
        public RealmDatabase GetRealm(WowRealm _Realm)
        {
            if (m_Realms.ContainsKey(_Realm) == false)
                m_Realms.Add(_Realm, new RealmDatabase(_Realm));
            return m_Realms[_Realm];
        }
        private bool m_NeedCleanup = false;
        public void Cleanup()
        {
            if (m_NeedCleanup == true)
            {
                foreach (var realm in m_Realms)
                {
                    if (realm.Value.IsLoadComplete() == false)
                        return;
                }
                GC.Collect();
                m_NeedCleanup = false;
            }
        }
        public void LoadRealmDatabase(string _RootPath, WowRealm _Realm, DateTime? _HistoryEarliestTime = null)
        {
            try
            {
                GetRealm(_Realm).LoadDatabase(_RootPath + _Realm.ToString() + "\\", _HistoryEarliestTime);
                m_NeedCleanup = true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
        public bool SaveRealmDatabases(string _RootPath, bool _ForceSave = false)
        {
            try
            {
                foreach (var realmDB in m_Realms)
                {
                    realmDB.Value.SaveDatabase(_RootPath + realmDB.Value.Realm.ToString() + "\\", _ForceSave);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return false;
        }
    }
}
