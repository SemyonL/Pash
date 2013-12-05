﻿// Copyright (C) Pash Contributors. License: GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Collections.ObjectModel;
using Pash.Implementation;
using System.Collections.Generic;
using System.Linq;

namespace System.Management.Automation
{
    public sealed class DriveManagementIntrinsics
    {
        private const string driveDoesntExistFormat = @"No such drive. A drive with the name ""{0}"" doesn't exist.";
        private const string driveAlreadyExistsFormat = @"A drive with the name ""{0}"" already exists.";

        private SessionState _sessionState;
        private SessionStateScope<PSDriveInfo> _scope;

        internal DriveManagementIntrinsics(SessionState sessionState, SessionStateScope<PSDriveInfo> driveScope)
        {
            _sessionState = sessionState;
            _scope = driveScope;
        }

        public PSDriveInfo Current
        {
            get
            {
                return _sessionState.SessionStateGlobal.CurrentDrive;
            }
        }

        public PSDriveInfo Get(string driveName)
        {
            var drive = _scope.Get(driveName, false);
            if (drive == null)
            {
                //PS 2.0 throws an exception here instead of returning null
                throw new MethodInvocationException(String.Format(driveDoesntExistFormat, driveName));
            }
            return drive;
        }

        public Collection<PSDriveInfo> GetAll()
        {
            return new Collection<PSDriveInfo>(_scope.GetAll().Values.ToList());
        }

        public Collection<PSDriveInfo> GetAllAtScope(string scope)
        {
            if (String.IsNullOrEmpty(scope))
            {
                 //this behavior corresponds to PS 2.0
                return GetAll();
            }
            return new Collection<PSDriveInfo>(_scope.GetAllAtScope(scope).Values.ToList());
        }

        public Collection<PSDriveInfo> GetAllForProvider(string providerName)
        {
            if (String.IsNullOrEmpty(providerName))
            {
                //this behavior corresponds to PS 2.0
                return GetAll();
            }
            /* "fun" fact: this method is different from getting the provider and checking its DriveProperty!
            * ProviderInfo.Drives seems to only provide drives that are available in the 0 scope
            * However, this functions needs to provide also the drives of child scopes that are associated with a
            * provider.
            * That's why sessionScope.SessionStateGlobal.GetProviderByName(providerName).Drives doesn't work here
            * At least that's the behavior of PS 2.0
            */
            var allDrives = GetAll();
            Collection<PSDriveInfo> providerDrives = new Collection<PSDriveInfo>();
            foreach (var drive in allDrives)
            {
                if (drive.Provider.IsNameMatch(providerName))
                {
                    providerDrives.Add(drive);
                }
            }
            //no drives means we don't have a proper provider!
            if (providerDrives.Count < 1)
            {
                throw new MethodInvocationException(
                    String.Format(@"No such provider. A provider with the name ""{0}"" doesn't exist.", providerName)
                );
            }
            return providerDrives;
        }

        public PSDriveInfo GetAtScope(string driveName, string scope)
        {
            var info =_scope.GetAtScope(driveName, scope);
            if (info == null)
            {
                throw new MethodInvocationException(String.Format(driveDoesntExistFormat, driveName));
            }
            return info;
        }

        public PSDriveInfo New(PSDriveInfo drive, string scope)
        {
            /*
             * "Fun" Fact: Although "private" is a valid scope specifier, it does not really make the drive
             * private, i.e. it does not restricts child scopes froma accessing or removing it.
             * "Private" seems to be only effective for variables, functions and aliases, but not for drives.
             * Who knows why.
             */
            _scope.SetAtScope(drive, scope, false);
            return drive;
        }

        public void Remove(string driveName, bool force, string scope)
        {
            /* TODO: force is used to remove the drive "although it's in use by the provider"
             * So, we need to find out when a drive is in use and should throw an exception on removal without
             * the "force" parameter being true
             */

            try
            {
                _scope.RemoveAtScope(driveName, scope);
            }
            catch (ItemNotFoundException)
            {
                throw new DriveNotFoundException(driveName, String.Empty, null);
            }
        }
    }
}
