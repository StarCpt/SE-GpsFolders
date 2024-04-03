﻿using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Collections;
using VRageMath;

namespace GpsFolders
{
    public class GpsFolderListView
    {
        public string LastSearchText { get; private set; }

        private List<MyGps> _allGpses;
        private SortedDictionary<string, FolderEntry> _folders;
        private FolderEntry _unsortedFolder;

        private GpsFolderListView()
        {
            _allGpses = new List<MyGps>();
            _folders = new SortedDictionary<string, FolderEntry>();
            _unsortedFolder = new FolderEntry("", "Unsorted");
        }

        public static GpsFolderListView Create(IEnumerable<MyGps> gpses)
        {
            var view = new GpsFolderListView();
            view._allGpses.AddRange(gpses);
            foreach (var gps in view._allGpses)
            {
                List<MyGps> target;
                if (gps.TryGetFolderId(out string folderName) && Extensions.IsFolderIdValid(folderName))
                {
                    if (!view._folders.TryGetValue(folderName, out FolderEntry folder))
                    {
                        view._folders.Add(folderName, folder = new FolderEntry(folderName));
                    }

                    target = folder.Entries;
                }
                else
                {
                    target = view._unsortedFolder.Entries;
                }

                target.OrderedInsert(gps, (x, y) =>
                {
                    if (x.Name == null)
                        return -1;
                    else if (y.Name == null)
                        return 1;
                    return x.Name.CompareTo(y.Name);
                });
            }
            return view;
        }

        public ListReader<MyGuiControlTable.Row> GetView(ref string currentFolderView, string searchText, bool expandAllFolders)
        {
            LastSearchText = searchText;

            if (currentFolderView != null)
            {
                if (currentFolderView == "")
                {
                    return GetFolderView(_unsortedFolder, searchText);
                }
                else if (_folders.TryGetValue(currentFolderView, out FolderEntry folder))
                {
                    return GetFolderView(folder, searchText);
                }
                else
                {
                    currentFolderView = null;
                    return GetRootView(searchText, expandAllFolders);
                }
            }

            return GetRootView(searchText, expandAllFolders);
        }

        private ListReader<MyGuiControlTable.Row> GetFolderView(FolderEntry folder, string searchText)
        {
            bool searchEmpty = String.IsNullOrWhiteSpace(searchText);
            string[] search = !searchEmpty ? searchText.Split(' ') : Array.Empty<string>();

            List<MyGuiControlTable.Row> items = new List<MyGuiControlTable.Row>
            {
                new GpsFolderRow(folder.FolderId, '…' + folder.DisplayName, Color.Yellow, MyGuiConstants.TEXTURE_ICON_BLUEPRINTS_LOCAL)
            };

            foreach (var item in folder.Entries)
            {
                if (searchEmpty ^ !Match(item))
                {
                    var row = new MyGuiControlTable.Row(item);
                    StringBuilder strb = new StringBuilder(item.Name);
                    Color color = item.DiscardAt.HasValue ? Color.Gray : (item.ShowOnHud ? item.GPSColor : Color.White);
                    row.AddCell(new MyGuiControlTable.Cell(strb, item, strb.ToString(), color));
                    items.Add(row);
                }
            }

            return new ListReader<MyGuiControlTable.Row>(items);

            bool Match(MyGps entry)
            {
                for (int i = 0; i < search.Length; i++)
                {
                    if (entry.Name.Contains(search[i], StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private ListReader<MyGuiControlTable.Row> GetRootView(string searchText, bool expandAllFolders)
        {
            List<MyGuiControlTable.Row> items = new List<MyGuiControlTable.Row>();

            bool searchEmpty = String.IsNullOrWhiteSpace(searchText);
            string[] search = !searchEmpty ? searchText.Split(' ') : Array.Empty<string>();

            foreach (var folder in _folders)
            {
                bool matchAny = false;
                int folderIndex = items.Count;
                foreach (var entry in folder.Value.Entries)
                {
                    if ((searchEmpty && expandAllFolders) || (!searchEmpty && Match(folder.Value.FolderId, entry.Name)))
                    {
                        matchAny = true;
                        AddGpsRow(entry);
                    }
                }

                if (matchAny || searchEmpty)
                {
                    AddFolderRow(folder.Value, folderIndex);
                }
            }

            if (_unsortedFolder.Entries.Count > 0)
            {
                bool matchAny = false;
                int folderIndex = items.Count;
                foreach (var entry in _unsortedFolder.Entries)
                {
                    if ((searchEmpty/* && expandAllFolders*/) || (!searchEmpty && Match(_unsortedFolder.DisplayName, entry.Name)))
                    {
                        matchAny = true;
                        AddGpsRow(entry);
                    }
                }

                if (matchAny || searchEmpty)
                {
                    AddFolderRow(_unsortedFolder, folderIndex);
                }
            }

            return new ListReader<MyGuiControlTable.Row>(items);

            void AddFolderRow(FolderEntry folder, int index)
            {
                items.Insert(index, new GpsFolderRow(folder.FolderId, folder.DisplayName, Color.Yellow, MyGuiConstants.TEXTURE_ICON_MODS_LOCAL));
            }

            void AddGpsRow(MyGps gps)
            {
                var row = new MyGuiControlTable.Row(gps);
                StringBuilder strb = new StringBuilder(gps.Name);
                Color color = gps.DiscardAt.HasValue ? Color.Gray : (gps.ShowOnHud ? gps.GPSColor : Color.White);
                row.AddCell(new MyGuiControlTable.Cell(strb, gps, strb.ToString(), color));
                items.Add(row);
            }

            bool Match(string folderId, string gpsName)
            {
                for (int i = 0; i < search.Length; i++)
                {
                    if (!gpsName.Contains(search[i], StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool TrySetFolderId(string folderId, string newId)
        {
            if (!Extensions.IsFolderIdValid(folderId) || !Extensions.IsFolderIdValid(newId) || folderId == newId)
            {
                return false;
            }

            if (!_folders.TryGetValue(folderId, out FolderEntry folder))
            {
                return false;
            }

            _folders.Remove(folderId);

            if (!_folders.TryGetValue(newId, out FolderEntry newFolder))
            {
                _folders.Add(newId, newFolder = new FolderEntry(newId));
            }

            foreach (var entry in folder.Entries)
            {
                newFolder.Entries.OrderedInsert(entry, (x, y) =>
                {
                    if (x.Name == null)
                        return -1;
                    else if (y.Name == null)
                        return 1;
                    return x.Name.CompareTo(y.Name);
                });

                entry.SetFolderId(newId);
            }

            return true;
        }

        public void DeleteFolder(string folderId)
        {
            if (!Helpers.TryGetFolderGpses(folderId, out IEnumerable<MyGps> gpsesToDelete))
            {
                return;
            }

            _folders.Remove(folderId);

            Helpers.ShowConfirmationDialog(
                "Delete Folder",
                "Are you sure you want to delete this folder and its contents?",
                result =>
                {
                    if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                    {
                        foreach (MyGps gps in gpsesToDelete)
                        {
                            MySession.Static.Gpss.SendDeleteGpsRequest(MySession.Static.LocalPlayerId, gps.Hash);
                        }
                    }
                });
        }

        private abstract class Item
        {

        }

        private class FolderEntry : Item
        {
            public string FolderId { get; }
            public string DisplayName { get; }
            public List<MyGps> Entries { get; }

            public FolderEntry(string id, string displayName = null)
            {
                FolderId = id ?? throw new ArgumentNullException(nameof(id));
                DisplayName = displayName ?? id;
                Entries = new List<MyGps>();
            }
        }
    }
}
