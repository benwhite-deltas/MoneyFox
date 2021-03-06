﻿using System.Threading.Tasks;

namespace MoneyFox.Foundation.Interfaces
{
    /// <summary>
    ///     Interface for Manager who manages automatic upload and download of backups
    /// </summary>
    public interface IAutobackupManager
    {
        Task RestoreBackupIfNewer();
        Task UploadBackupIfNewer();
    }
}