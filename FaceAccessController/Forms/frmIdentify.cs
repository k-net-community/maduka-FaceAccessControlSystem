﻿using FaceAccessController.ClassLibrary;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.IO;

namespace FaceAccessController.Forms
{
    public partial class frmIdentify : Base.BaseForm
    {
        FaceServiceClient face;
        IDictionary DicPerson;
        List<Rectangle> rect = new List<Rectangle>();

        public frmIdentify()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 頁面載入的動作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmIdentify_Load(object sender, EventArgs e)
        {
            base.ReadConfig();
            face = new FaceServiceClient(base.SetupConfig.FaceApiKey);
            new CognitiveUtility().BindPersonGroup(cbxPersonGroup, face, "");
        }

        /// <summary>
        /// 點選打開檔案讀取的動作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            fileDialog.ShowDialog();
        }

        /// <summary>
        /// 決定選好的照片的動作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileDialog_FileOk(object sender, CancelEventArgs e)
        {
            txtFileName.Text = fileDialog.FileName;
            plTag.BackgroundImage = Image.FromFile(txtFileName.Text);
            plTag.BackgroundImageLayout = ImageLayout.Stretch;
            rect.Clear();
            plTag.Invalidate();
        }

        /// <summary>
        /// 將人員群組的人員資料放入到字典檔中的動作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void cbxPersonGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            string strPersonGroupId = ((Models.CognitiveModels.ListItem)cbxPersonGroup.SelectedItem).Value;
            DicPerson = new Hashtable();
            Person[] objPersons = await face.GetPersonsAsync(strPersonGroupId);

            for (int i = 0; i < objPersons.Length; i++)
                DicPerson.Add(objPersons[i].PersonId.ToString().Replace("{", "").Replace("}", ""), objPersons[i].Name);
        }

        /// <summary>
        /// 點選上傳的動作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnUpload_Click(object sender, EventArgs e)
        {
            using (Stream imageFileStream = File.OpenRead(txtFileName.Text))
            {
                Face[] faces = await face.DetectAsync(imageFileStream);
                await this.DetectFace(faces);
            }
        }

        private async Task DetectFace(Face[] faces)
        {
            List<Face> objAddPanelFaces = new List<Face>();

            // 將照片中的臉，與指定的PersonGroup進行比對
            if (faces != null)
            {
                Guid[] faceGuids = faces.Select(x => x.FaceId).ToArray();
                if (faceGuids.Length > 0)
                {
                    string strPersonGroup = ((Models.CognitiveModels.ListItem)cbxPersonGroup.SelectedItem).Value;
                    try
                    {
                        IdentifyResult[] result = await face.IdentifyAsync(strPersonGroup, faceGuids);
                        txtResult.Text = JsonConvert.SerializeObject(result);

                        // 取得照片中的人臉
                        string strPersonNameLabel = "";
                        for (int i = 0; i < result.Length; i++)
                        {
                            for (int p = 0; p < result[i].Candidates.Length; p++)
                            {
                                string strPersonId = result[i].Candidates[p].PersonId.ToString();
                                string strPersonName = (DicPerson.Contains(strPersonId)) ? DicPerson[strPersonId].ToString() : "";
                                strPersonNameLabel += strPersonName + ",";

                                objAddPanelFaces.Add(faces[i]);
                            }
                        }
                        txtPerson.Text = strPersonNameLabel;
                    }
                    catch (Exception e)
                    {
                        string strErrMsg = e.Message;
                    }
                }
            }

            // 加入畫出內容的方框
            this.RenderRectangle(objAddPanelFaces.ToArray());
        }

        private void RenderRectangle(Face[] results)
        {
            rect.Clear();

            float floPhysicalHeight = plTag.BackgroundImage.PhysicalDimension.Height;
            float floPhysicalWidth = plTag.BackgroundImage.PhysicalDimension.Width;

            // 取得事件的x,y以及height, width
            int intVedioWidth = plTag.Width;
            int intVedioHeight = plTag.Height;

            for (int i = 0; i < results.Length; i++)
            {

                // 找出方框出現的X與Y
                int intX = (int)(intVedioWidth * results[i].FaceRectangle.Left / floPhysicalWidth);
                int intY = (int)(intVedioHeight * results[i].FaceRectangle.Top / floPhysicalHeight);
                //intX = axWindowsMediaPlayer1.Left + intX;
                //intY = axWindowsMediaPlayer1.Top + intY;

                // 找出方框的高度與寬度
                int intHeight = (int)(intVedioHeight * results[i].FaceRectangle.Height / floPhysicalHeight);
                int intWidth = (int)(intVedioWidth * results[i].FaceRectangle.Width / floPhysicalWidth);

                rect.Add(new Rectangle(intX, intY, intHeight, intWidth));
            }

            plTag.BackgroundImage = Image.FromFile(txtFileName.Text);
            plTag.BackgroundImageLayout = ImageLayout.Stretch;
            plTag.Invalidate();
        }

        private void plTag_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.Green, 1))
            {
                for (int i = 0; i < rect.Count; i++)
                    e.Graphics.DrawRectangle(pen, rect[i]);
            }
        }
    }
}