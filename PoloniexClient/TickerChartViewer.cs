﻿using CryptoMarketClient.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraCharts;
using DevExpress.XtraBars;
using DevExpress.Skins;
using DevExpress.Utils.Svg;
using DevExpress.LookAndFeel;
using DevExpress.XtraEditors;

namespace CryptoMarketClient {
    public partial class TickerChartViewer : XtraUserControl {
        public TickerChartViewer() {
            InitializeComponent();
            InitializeCheckItems();
            SetCandleStickCheckItemValues();
            this.barManager1.ForceInitialize();
        }

        protected Form MdiParentForm { get; set; }
        protected Form ParentFormCore { get; set; }

        TickerBase ticker;
        public TickerBase Ticker {
            get { return ticker; }
            set {
                if(Ticker == value)
                    return;
                TickerBase prev = ticker;
                this.ticker = value;
                OnTickerChanged(prev);
            }
        }
        void OnTickerChanged(TickerBase prev) {
            if(prev != null) {
                prev.OrderBook.OnChanged -= OrderBook_OnChanged;
                prev.HistoryItemAdd -= Ticker_HistoryItemAdd;
                prev.EventsChanged -= Settings_EventsChanged;
            }
            if(Ticker != null) {
                Ticker.OrderBook.OnChanged += OrderBook_OnChanged;
                Ticker.HistoryItemAdd += Ticker_HistoryItemAdd;
                Ticker.EventsChanged += Settings_EventsChanged;
            }
            if(Ticker != null) {
                UpdateDataFromServer();
                UpdateCandleStickMenu();
                UpdateChart();
            }
        }
        void UpdateCandleStickMenu() {
            if(Ticker == null)
                return;
            foreach(BarCheckItemLink link in this.bsCandleStickPeriod.ItemLinks) {
                if(((int)link.Item.Tag) == Ticker.CandleStickPeriodMin) {
                    link.Item.Down = true;
                    this.bsCandleStickPeriod.Caption = link.Item.Caption;
                    break;
                }
            }
        }

        private void Ticker_HistoryItemAdd(object sender, EventArgs e) {
            CandleStickChartHelper.AddCandleStickData(Ticker.CandleStickData, Ticker.History[Ticker.History.Count - 1], Ticker.CandleStickPeriodMin * 60);
        }

        private void OrderBook_OnChanged(object sender, OrderBookEventArgs e) {

        }

        void CreateCandleStickDataSource() {
            Ticker.CandleStickData = CandleStickChartHelper.CreateCandleStickData(Ticker.History, Ticker.CandleStickPeriodMin * 60);
        }

        Series CreateLineSeries(List<OrderBookStatisticItem> list, string str, Color color) {
            Series s = new Series();
            s.Name = str;
            s.ArgumentDataMember = "Time";
            s.ValueDataMembers.AddRange(str);
            s.ValueScaleType = ScaleType.Numerical;
            s.ShowInLegend = true;
            StepLineSeriesView view = new StepLineSeriesView();
            view.Color = color;
            view.LineStyle.Thickness = (int)(1 * DpiProvider.Default.DpiScaleFactor);
            s.View = view;
            s.DataSource = list;
            return s;
        }

        Series CreateStepAreaSeries(List<OrderBookStatisticItem> list, string str, Color color) {
            Series s = new Series();
            s.Name = str;
            s.ArgumentDataMember = "Time";
            s.ValueDataMembers.AddRange(str);
            s.ValueScaleType = ScaleType.Numerical;
            s.ShowInLegend = true;
            StepAreaSeriesView view = new StepAreaSeriesView();
            view.Color = color;
            s.View = view;
            s.DataSource = list;
            return s;
        }

        Series CreateAreaSeries(List<TickerHistoryItem> list, string str, Color color) {
            Series s = new Series();
            s.Name = str;
            s.ArgumentDataMember = "Time";
            s.ValueDataMembers.AddRange(str);
            s.ValueScaleType = ScaleType.Numerical;
            s.ShowInLegend = true;
            AreaSeriesView view = new AreaSeriesView();
            view.Color = color;
            s.View = view;
            s.DataSource = list;
            return s;
        }

        Series CreateBarSeries(List<OrderBookStatisticItem> list, string name, string value, Color color) {
            Series s = new Series();
            s.Name = name;
            s.ArgumentDataMember = "Time";
            s.ValueDataMembers.AddRange(value);
            s.ValueScaleType = ScaleType.Numerical;
            s.ShowInLegend = true;
            SideBySideBarSeriesView view = new SideBySideBarSeriesView();
            view.Color = color;
            view.AxisY = ((XYDiagram)this.chartControl1.Diagram).SecondaryAxesY["Hipes"];
            s.View = view;
            s.DataSource = list;
            return s;
        }

        Series CreateCandleStickSeries() {
            Series s = new Series("Last", ViewType.CandleStick);
            s.ArgumentDataMember = "Time";
            s.ArgumentScaleType = ScaleType.DateTime;
            s.ValueDataMembers.AddRange("Low", "High", "Open", "Close");
            s.ValueScaleType = ScaleType.Numerical;
            CandleStickSeriesView view = new CandleStickSeriesView();

            view.LineThickness = (int)(1 * DpiProvider.Default.DpiScaleFactor);
            view.LevelLineLength = 0.25;
            view.ReductionOptions.ColorMode = ReductionColorMode.OpenToCloseValue;
            view.ReductionOptions.FillMode = CandleStickFillMode.FilledOnReduction;
            view.ReductionOptions.Level = StockLevel.Open;
            view.ReductionOptions.Visible = true;

            s.View = view;
            s.DataSource = Ticker.CandleStickData;
            return s;
        }

        Series CreateStockSeries() {
            Series s = new Series("Last", ViewType.Stock);
            s.ArgumentDataMember = "Time";
            s.ArgumentScaleType = ScaleType.DateTime;
            s.ValueDataMembers.AddRange("Low", "High", "Open", "Close");
            s.ValueScaleType = ScaleType.Numerical;
            StockSeriesView view = new StockSeriesView();

            view.ShowOpenClose = StockType.Both;
            view.LineThickness = 2;
            view.LevelLineLength = 0.25;
            view.ReductionOptions.ColorMode = ReductionColorMode.OpenToCloseValue;
            view.ReductionOptions.Level = StockLevel.Open;
            view.ReductionOptions.Visible = true;

            s.View = view;
            s.DataSource = Ticker.CandleStickData;
            return s;
        }

        protected Series BidSeries { get; set; }
        protected Series AskSeries { get; set; }
        protected Series CurrentSeries { get; set; }

        void UpdateChart() {
            if(Ticker == null)
                return;
            this.chartControl1.Series["Sell volume"].DataSource = Ticker.TradeStatistic;
            this.chartControl1.Series["Sell volume"].ArgumentDataMember = "Time";
            this.chartControl1.Series["Sell volume"].ValueDataMembers.AddRange("SellVolume");
            this.chartControl1.Series["Buy volume"].DataSource = Ticker.TradeStatistic;
            this.chartControl1.Series["Buy volume"].ArgumentDataMember = "Time";
            this.chartControl1.Series["Buy volume"].ValueDataMembers.AddRange("BuyVolume");

            this.chartControl1.Series["Volume"].ArgumentDataMember = "Time";
            this.chartControl1.Series["Volume"].ValueDataMembers.AddRange("Volume");

            ConfigurateChart(ViewType.CandleStick);
            UpdateEvents(null);
        }
        Series CreateLastSeries() {
            if(this.bcStock.Checked)
                return CreateStockSeries();
            else if(this.bcColoredCandle.Checked)
                return CreateCandleStickSeries();
            else if(this.bcLine.Checked)
                return CreateLineSeries(Ticker.OrderBook.VolumeHistory, "Current", Color.DarkGray);
            else if(this.bcCandle.Checked)
                return CreateCandleStickSeries();
            else if(this.bcColoredStock.Checked)
                return CreateStockSeries();
            else if(this.bcArea.Checked)
                return CreateStepAreaSeries(Ticker.OrderBook.VolumeHistory, "Current", Color.DarkGray);
            return null;
        }

        private void OnCandleStickPeriodChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e) {
            if(!((BarCheckItem)e.Item).Checked)
                return;
            this.bsCandleStickPeriod.Caption = e.Item.Caption;
            if(Ticker.CandleStickPeriodMin == (int)e.Item.Tag)
                return;
            Ticker.CandleStickPeriodMin = (int)e.Item.Tag;
            Ticker.CandleStickData.Clear();
            UpdateDataFromServer();
        }
        void SetCandleStickCheckItemValues() {
            this.bcFifteenMinutes.Tag = 15;
            this.bcFiveMinutes.Tag = 5;
            this.bcOneDay.Tag = 24 * 60;
            this.bcOneHour.Tag = 60;
            this.bcOneMinute.Tag = 1;
            this.bcOneMonth.Tag = 30 * 24 * 60;
            this.bcOneWeek.Tag = 7 * 24 * 60;
            this.bcThirtyMinutes.Tag = 30;
            this.bcThreeDays.Tag = 3 * 24 * 60;
        }

        private void OnChartTypeChanged(object sender, ItemClickEventArgs e) {
            if(!((BarCheckItem)e.Item).Checked)
                return;
            ConfigurateChart((ViewType)e.Item.Tag);
        }
        void ConfigurateChart(ViewType type) {
            this.chartControl1.Series["Current"].ChangeView(type);
            this.chartControl1.Series["Current"].DataSource = null;
            if(type == ViewType.CandleStick || type == ViewType.Stock)
                this.chartControl1.Series["Current"].BindToData(Ticker.CandleStickData, "Time", "Low", "High", "Open", "Close");
            else
                this.chartControl1.Series["Current"].BindToData(Ticker.History, "Time", "Current");
            UpdateChartProperties();
        }
        void InitializeCheckItems() {
            this.bcStock.Tag = ViewType.Stock;
            this.bcLine.Tag = ViewType.Line;
            this.bcCandle.Tag = ViewType.CandleStick;
        }

        protected void UpdateDataFromServer() {
            int hours = 100 * 24; // 100 days
            DateTime start = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(hours * 60 * 60));
            BindingList<CandleStickData> data = Ticker.GetCandleStickData(Ticker.CandleStickPeriodMin, start, hours * 60 * 60);
            Ticker.CandleStickData = data;
            this.chartControl1.Series["Current"].DataSource = data;
            this.chartControl1.Series["Volume"].DataSource = data;
            UpdateChartProperties();
        }

        protected void UpdateChartProperties() {
            ((BarSeriesView)this.chartControl1.Series["Volume"].View).BarWidth = 0.6 * Ticker.CandleStickPeriodMin;
            ((BarSeriesView)this.chartControl1.Series["Volume"].View).Border.Visibility = DevExpress.Utils.DefaultBoolean.False;
            ((FinancialSeriesViewBase)this.chartControl1.Series["Current"].View).LevelLineLength = 0.6 / 2 * Ticker.CandleStickPeriodMin;
        }

        private void barButtonItem3_ItemClick(object sender, ItemClickEventArgs e) {
            UpdateDataFromServer();
        }
        public void AddIndicator(TrailingSettings settings) {
            if(Ticker == null)
                return;
            UpdateEvents(null);
        }

        private void Settings_EventsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            UpdateEvents(e);
        }

        protected SeriesPoint CreateEventPoint(TickerEvent ev) {
            SeriesPoint pt = new SeriesPoint(ev.Time, ev.Current);
            pt.ToolTipHint = ev.Text;
            pt.Tag = ev;
            pt.Color = ev.Color;
            return pt;
        }

        void UpdateEvents(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            Series s = this.chartControl1.Series["Events"];
            if(e == null || e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add) {
                s.Points.Clear();
                foreach(TickerEvent ev in Ticker.Events)
                    s.Points.Add(CreateEventPoint(ev));
            }
            else {
                foreach(TickerEvent ev in e.NewItems)
                    s.Points.Add(CreateEventPoint(ev));
            }
        }

        public void RemoveIndicator(TrailingSettings settings) {
            if(Ticker == null)
                return;
            UpdateEvents(null);
        }
    }
}