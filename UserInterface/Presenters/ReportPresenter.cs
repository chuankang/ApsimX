﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models;
using UserInterface.Views;
using System.Reflection;
using Models.Core;

namespace UserInterface.Presenters
{
    class ReportPresenter : IPresenter
    {
        private Report Report;
        private IReportView View;
        private ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Attach the model (report) and the view (IReportView)
        /// </summary>
        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            this.Report = Model as Report;
            this.ExplorerPresenter = explorerPresenter;
            this.View = View as IReportView;

            this.View.VariableList.Lines = Report.Variables;
            this.View.EventList.Lines = Report.Events;
            this.View.VariableList.ContextItemsNeeded += OnNeedVariableNames;
            this.View.EventList.ContextItemsNeeded += OnNeedEventNames;
            this.View.VariableList.TextHasChangedByUser += OnVariableNamesChanged;
            this.View.EventList.TextHasChangedByUser += OnEventNamesChanged;
            ExplorerPresenter.CommandHistory.ModelChanged += CommandHistory_ModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.View.VariableList.ContextItemsNeeded -= OnNeedVariableNames;
            this.View.EventList.ContextItemsNeeded -= OnNeedEventNames;
            this.View.VariableList.TextHasChangedByUser -= OnVariableNamesChanged;
            this.View.EventList.TextHasChangedByUser -= OnEventNamesChanged;
            ExplorerPresenter.CommandHistory.ModelChanged -= CommandHistory_ModelChanged;
        }

        /// <summary>
        /// The view is asking for variable names.
        /// </summary>
        void OnNeedVariableNames(object Sender, Utility.NeedContextItems e)
        {
            if (e.ObjectName == "")
                e.ObjectName = ".";
            object o = Report.Get(e.ObjectName);

            if (o != null)
            {
                foreach (Utility.IVariable Property in Utility.ModelFunctions.FieldsAndProperties(o, BindingFlags.Instance | BindingFlags.Public))
                {
                    e.Items.Add(Property.Name);
                }
                e.Items.Sort();
            }
        }

        /// <summary>
        /// The view is asking for event names.
        /// </summary>
        void OnNeedEventNames(object Sender, Utility.NeedContextItems e)
        {
            object o = Report.Get(e.ObjectName);

            if (o != null)
            {
                foreach (EventInfo Event in o.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public))
                    e.Items.Add(Event.Name);
            }
        }

        /// <summary>
        /// The variable names have changed in the view.
        /// </summary>
        void OnVariableNamesChanged(object sender, EventArgs e)
        {
            ExplorerPresenter.CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
            ExplorerPresenter.CommandHistory.Add(new Commands.ChangePropertyCommand(Report, "Variables", View.VariableList.Lines));
            ExplorerPresenter.CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
        }

        /// <summary>
        /// The event names have changed in the view.
        /// </summary>
        void OnEventNamesChanged(object sender, EventArgs e)
        {
            ExplorerPresenter.CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
            ExplorerPresenter.CommandHistory.Add(new Commands.ChangePropertyCommand(Report, "Events", View.EventList.Lines));
            ExplorerPresenter.CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
        }

        /// <summary>
        /// The model has changed so update our view.
        /// </summary>
        void CommandHistory_ModelChanged(object changedModel)
        {
            if (changedModel == Report)
            {
                View.VariableList.Lines = Report.Variables;
                View.EventList.Lines = Report.Events;
            }
        }


    }
}
