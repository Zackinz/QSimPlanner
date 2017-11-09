﻿using QSP.RouteFinding.TerminalProcedures;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using QSP.UI.Util;

namespace QSP.UI.Views.Route
{
    // Load the ProcedureFilter, and let user choose the settings.
    // Then ProcedureFilter is modified.
    //
    public partial class SidStarFilterControl : UserControl
    {
        public event EventHandler FinishedSelection;

        private string icao;
        private string rwy;
        private List<string> procedures;
        private bool isSid;
        private ProcedureFilter procFilter;
        private List<ListViewItem> items;

        public SidStarFilterControl()
        {
            InitializeComponent();
        }

        public void Init(
            string icao,
            string rwy,
            List<string> procedures,
            bool isSid,
            ProcedureFilter procFilter)
        {
            this.icao = icao;
            this.rwy = rwy;
            this.procedures = procedures;
            this.isSid = isSid;
            this.procFilter = procFilter;

            SetType();
            SetListView();

            showSelectedCheckBox.CheckedChanged += ShowSelectedCheckBoxChanged;

            okBtn.Click += UpdateFilter;
            cancelBtn.Click += (sender, e) => FinishedSelection?.Invoke(sender, e);

            new ListViewSortEnabler(procListView).EnableSort();
        }

        private IEnumerable<string> CheckedItems()
        {
            return items
                .Where(i => i.Checked)
                .Select(i => i.Text);
        }

        private void UpdateFilter(object sender, EventArgs e)
        {
            procFilter[icao, rwy] = new FilterEntry(
                listTypeComboBox.SelectedIndex == 0,
                CheckedItems().ToList());

            FinishedSelection?.Invoke(sender, e);
        }

        private void SetType()
        {
            listTypeComboBox.Items.Clear();
            listTypeComboBox.Items.AddRange(new []{ "Blacklist", "Whitelist" });

            listTypeComboBox.SelectedIndex = 0;
        }

        private void SetListView()
        {
            procListView.Columns[0].Text = isSid ? "SID" : "STAR";
            procListView.CheckBoxes = true;

            var items = procListView.Items;

            items.Clear();

            foreach (var i in procedures)
            {
                items.Add(new ListViewItem(i));
            }

            this.items = items.Cast<ListViewItem>().ToList();
            procListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            TickCheckBoxes();
        }

        private void TickCheckBoxes()
        {
            if (procFilter.Exists(icao, rwy))
            {
                var info = procFilter[icao, rwy];
                listTypeComboBox.SelectedIndex = info.IsBlackList ? 0 : 1;

                foreach (var i in items)
                {
                    i.Checked = info.Procedures.Contains(i.Text);
                }
            }
        }

        private void Filter(Func<ListViewItem, bool> predicate)
        {
            var selected = items.Where(predicate).ToArray();
            procListView.Items.Clear();
            procListView.Items.AddRange(selected);
        }

        private void ShowSelectedCheckBoxChanged(object sender, EventArgs e)
        {
            if (showSelectedCheckBox.Checked)
            {
                Filter(i => i.Checked);
            }
            else
            {
                Filter(i => true);
            }
        }
    }
}