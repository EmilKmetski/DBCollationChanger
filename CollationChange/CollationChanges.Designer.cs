namespace CollationChange
{
    partial class CollationChanges
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CollationChanges));
            this.cbCollation = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lbColInfo = new System.Windows.Forms.Label();
            this.lvDBCollation = new System.Windows.Forms.ListView();
            this.DBName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.dbCollation = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label3 = new System.Windows.Forms.Label();
            this.btExecute = new System.Windows.Forms.Button();
            this.btCleanSelection = new System.Windows.Forms.Button();
            this.lbCodepage = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lbCollation = new System.Windows.Forms.Label();
            this.cbkAll = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // cbCollation
            // 
            this.cbCollation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCollation.FormattingEnabled = true;
            this.cbCollation.Location = new System.Drawing.Point(430, 40);
            this.cbCollation.Name = "cbCollation";
            this.cbCollation.Size = new System.Drawing.Size(302, 21);
            this.cbCollation.TabIndex = 0;
            this.cbCollation.SelectedIndexChanged += new System.EventHandler(this.cbCollation_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(427, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "New Collation";
            // 
            // lbColInfo
            // 
            this.lbColInfo.Location = new System.Drawing.Point(430, 78);
            this.lbColInfo.Name = "lbColInfo";
            this.lbColInfo.Size = new System.Drawing.Size(302, 166);
            this.lbColInfo.TabIndex = 2;
            this.lbColInfo.Text = " ";
            // 
            // lvDBCollation
            // 
            this.lvDBCollation.CheckBoxes = true;
            this.lvDBCollation.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.DBName,
            this.dbCollation});
            this.lvDBCollation.Location = new System.Drawing.Point(12, 28);
            this.lvDBCollation.Name = "lvDBCollation";
            this.lvDBCollation.Size = new System.Drawing.Size(409, 242);
            this.lvDBCollation.TabIndex = 3;
            this.lvDBCollation.UseCompatibleStateImageBehavior = false;
            this.lvDBCollation.View = System.Windows.Forms.View.Details;
            // 
            // DBName
            // 
            this.DBName.Text = "Database Name";
            this.DBName.Width = 200;
            // 
            // dbCollation
            // 
            this.dbCollation.Text = "Collation";
            this.dbCollation.Width = 200;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(55, 8);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(158, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Databses with different Collation";
            // 
            // btExecute
            // 
            this.btExecute.Location = new System.Drawing.Point(657, 247);
            this.btExecute.Name = "btExecute";
            this.btExecute.Size = new System.Drawing.Size(75, 23);
            this.btExecute.TabIndex = 5;
            this.btExecute.Text = "Execute";
            this.btExecute.UseVisualStyleBackColor = true;
            this.btExecute.Click += new System.EventHandler(this.btExecute_Click);
            // 
            // btCleanSelection
            // 
            this.btCleanSelection.Location = new System.Drawing.Point(430, 247);
            this.btCleanSelection.Name = "btCleanSelection";
            this.btCleanSelection.Size = new System.Drawing.Size(92, 23);
            this.btCleanSelection.TabIndex = 6;
            this.btCleanSelection.Text = "Clear selection";
            this.btCleanSelection.UseVisualStyleBackColor = true;
            this.btCleanSelection.Click += new System.EventHandler(this.btCleanSelection_Click);
            // 
            // lbCodepage
            // 
            this.lbCodepage.Location = new System.Drawing.Point(430, 64);
            this.lbCodepage.Name = "lbCodepage";
            this.lbCodepage.Size = new System.Drawing.Size(302, 14);
            this.lbCodepage.TabIndex = 7;
            this.lbCodepage.Text = " ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(343, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Server Collation";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // lbCollation
            // 
            this.lbCollation.ForeColor = System.Drawing.Color.DarkRed;
            this.lbCollation.Location = new System.Drawing.Point(430, 9);
            this.lbCollation.Name = "lbCollation";
            this.lbCollation.Size = new System.Drawing.Size(302, 13);
            this.lbCollation.TabIndex = 9;
            this.lbCollation.Text = " ";
            // 
            // cbkAll
            // 
            this.cbkAll.AutoSize = true;
            this.cbkAll.Location = new System.Drawing.Point(12, 8);
            this.cbkAll.Name = "cbkAll";
            this.cbkAll.Size = new System.Drawing.Size(37, 17);
            this.cbkAll.TabIndex = 10;
            this.cbkAll.Text = "All";
            this.cbkAll.UseVisualStyleBackColor = true;
            // 
            // CollationChanges
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(744, 282);
            this.Controls.Add(this.cbkAll);
            this.Controls.Add(this.lbCollation);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lbCodepage);
            this.Controls.Add(this.btCleanSelection);
            this.Controls.Add(this.btExecute);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lvDBCollation);
            this.Controls.Add(this.lbColInfo);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbCollation);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CollationChanges";
            this.Text = "Databases Collaiton Changer";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cbCollation;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lbColInfo;
        private System.Windows.Forms.ListView lvDBCollation;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btExecute;
        private System.Windows.Forms.Button btCleanSelection;
        private System.Windows.Forms.ColumnHeader DBName;
        private System.Windows.Forms.ColumnHeader dbCollation;
        private System.Windows.Forms.Label lbCodepage;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lbCollation;
        private System.Windows.Forms.CheckBox cbkAll;
    }
}

