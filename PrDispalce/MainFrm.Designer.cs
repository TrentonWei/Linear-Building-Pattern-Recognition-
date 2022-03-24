namespace PrDispalce
{
    partial class MainFrm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainFrm));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.建筑物特征计算ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.协同移位整体ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.图形剖分ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.直线模式识别ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.knowledgegraphSupportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.移除图层ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.axLicenseControl1 = new ESRI.ArcGIS.Controls.AxLicenseControl();
            this.axMapControl1 = new ESRI.ArcGIS.Controls.AxMapControl();
            this.axTOCControl1 = new ESRI.ArcGIS.Controls.AxTOCControl();
            this.axToolbarControl1 = new ESRI.ArcGIS.Controls.AxToolbarControl();
            this.relationComputationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axLicenseControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axMapControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axTOCControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axToolbarControl1)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.建筑物特征计算ToolStripMenuItem,
            this.协同移位整体ToolStripMenuItem,
            this.relationComputationToolStripMenuItem,
            this.图形剖分ToolStripMenuItem,
            this.直线模式识别ToolStripMenuItem,
            this.knowledgegraphSupportToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(8, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(1710, 28);
            this.menuStrip1.TabIndex = 5;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 建筑物特征计算ToolStripMenuItem
            // 
            this.建筑物特征计算ToolStripMenuItem.Name = "建筑物特征计算ToolStripMenuItem";
            this.建筑物特征计算ToolStripMenuItem.Size = new System.Drawing.Size(150, 24);
            this.建筑物特征计算ToolStripMenuItem.Text = "BuildingMeasures";
            this.建筑物特征计算ToolStripMenuItem.Click += new System.EventHandler(this.建筑物特征计算ToolStripMenuItem_Click);
            // 
            // 协同移位整体ToolStripMenuItem
            // 
            this.协同移位整体ToolStripMenuItem.Name = "协同移位整体ToolStripMenuItem";
            this.协同移位整体ToolStripMenuItem.Size = new System.Drawing.Size(134, 24);
            this.协同移位整体ToolStripMenuItem.Text = "ProximityGraph";
            this.协同移位整体ToolStripMenuItem.Click += new System.EventHandler(this.协同移位整体ToolStripMenuItem_Click);
            // 
            // 图形剖分ToolStripMenuItem
            // 
            this.图形剖分ToolStripMenuItem.Name = "图形剖分ToolStripMenuItem";
            this.图形剖分ToolStripMenuItem.Size = new System.Drawing.Size(193, 24);
            this.图形剖分ToolStripMenuItem.Text = "PolygonDecomposition";
            this.图形剖分ToolStripMenuItem.Click += new System.EventHandler(this.图形剖分ToolStripMenuItem_Click);
            // 
            // 直线模式识别ToolStripMenuItem
            // 
            this.直线模式识别ToolStripMenuItem.Name = "直线模式识别ToolStripMenuItem";
            this.直线模式识别ToolStripMenuItem.Size = new System.Drawing.Size(207, 24);
            this.直线模式识别ToolStripMenuItem.Text = "LinearPatternRecognition";
            this.直线模式识别ToolStripMenuItem.Click += new System.EventHandler(this.直线模式识别ToolStripMenuItem_Click);
            // 
            // knowledgegraphSupportToolStripMenuItem
            // 
            this.knowledgegraphSupportToolStripMenuItem.Name = "knowledgegraphSupportToolStripMenuItem";
            this.knowledgegraphSupportToolStripMenuItem.Size = new System.Drawing.Size(214, 24);
            this.knowledgegraphSupportToolStripMenuItem.Text = "KnowledgeGraph-Support";
            this.knowledgegraphSupportToolStripMenuItem.Click += new System.EventHandler(this.knowledgegraphSupportToolStripMenuItem_Click);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.移除图层ToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(139, 28);
            // 
            // 移除图层ToolStripMenuItem
            // 
            this.移除图层ToolStripMenuItem.Name = "移除图层ToolStripMenuItem";
            this.移除图层ToolStripMenuItem.Size = new System.Drawing.Size(138, 24);
            this.移除图层ToolStripMenuItem.Text = "移除图层";
            this.移除图层ToolStripMenuItem.Click += new System.EventHandler(this.移除图层ToolStripMenuItem_Click);
            // 
            // axLicenseControl1
            // 
            this.axLicenseControl1.Enabled = true;
            this.axLicenseControl1.Location = new System.Drawing.Point(740, 376);
            this.axLicenseControl1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.axLicenseControl1.Name = "axLicenseControl1";
            this.axLicenseControl1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axLicenseControl1.OcxState")));
            this.axLicenseControl1.Size = new System.Drawing.Size(32, 32);
            this.axLicenseControl1.TabIndex = 9;
            // 
            // axMapControl1
            // 
            this.axMapControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axMapControl1.Location = new System.Drawing.Point(264, 56);
            this.axMapControl1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.axMapControl1.Name = "axMapControl1";
            this.axMapControl1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axMapControl1.OcxState")));
            this.axMapControl1.Size = new System.Drawing.Size(1446, 706);
            this.axMapControl1.TabIndex = 8;
            // 
            // axTOCControl1
            // 
            this.axTOCControl1.Dock = System.Windows.Forms.DockStyle.Left;
            this.axTOCControl1.Location = new System.Drawing.Point(0, 56);
            this.axTOCControl1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.axTOCControl1.Name = "axTOCControl1";
            this.axTOCControl1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axTOCControl1.OcxState")));
            this.axTOCControl1.Size = new System.Drawing.Size(264, 706);
            this.axTOCControl1.TabIndex = 7;
            this.axTOCControl1.OnMouseDown += new ESRI.ArcGIS.Controls.ITOCControlEvents_Ax_OnMouseDownEventHandler(this.axTOCControl1_OnMouseDown_1);
            // 
            // axToolbarControl1
            // 
            this.axToolbarControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.axToolbarControl1.Location = new System.Drawing.Point(0, 28);
            this.axToolbarControl1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.axToolbarControl1.Name = "axToolbarControl1";
            this.axToolbarControl1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axToolbarControl1.OcxState")));
            this.axToolbarControl1.Size = new System.Drawing.Size(1710, 28);
            this.axToolbarControl1.TabIndex = 6;
            // 
            // relationComputationToolStripMenuItem
            // 
            this.relationComputationToolStripMenuItem.Name = "relationComputationToolStripMenuItem";
            this.relationComputationToolStripMenuItem.Size = new System.Drawing.Size(177, 24);
            this.relationComputationToolStripMenuItem.Text = "RelationComputation";
            this.relationComputationToolStripMenuItem.Click += new System.EventHandler(this.relationComputationToolStripMenuItem_Click);
            // 
            // MainFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1710, 762);
            this.Controls.Add(this.axLicenseControl1);
            this.Controls.Add(this.axMapControl1);
            this.Controls.Add(this.axTOCControl1);
            this.Controls.Add(this.axToolbarControl1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "MainFrm";
            this.Text = "Form1";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.axLicenseControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axMapControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axTOCControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axToolbarControl1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 移除图层ToolStripMenuItem;
        private ESRI.ArcGIS.Controls.AxToolbarControl axToolbarControl1;
        private ESRI.ArcGIS.Controls.AxTOCControl axTOCControl1;
        private ESRI.ArcGIS.Controls.AxMapControl axMapControl1;
        private ESRI.ArcGIS.Controls.AxLicenseControl axLicenseControl1;
        private System.Windows.Forms.ToolStripMenuItem 协同移位整体ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 图形剖分ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 建筑物特征计算ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 直线模式识别ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem knowledgegraphSupportToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem relationComputationToolStripMenuItem;
    }
}

