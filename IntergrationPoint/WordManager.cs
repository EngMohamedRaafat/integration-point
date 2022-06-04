using System.IO;
using System.Reflection;
using Word = Microsoft.Office.Interop.Word;

namespace IntergrationPoint
{
    class WordManager
    {
        //Find and Replace Method
        private static void FindAndReplace(Word.Application wordApp, object ToFindText, object replaceWithText)
        {
            object matchCase = true;
            object matchWholeWord = true;
            object matchWildCards = false;
            object matchSoundLike = false;
            object nmatchAllforms = false;
            object forward = true;
            object format = false;
            object matchKashida = false;
            object matchDiactitics = false;
            object matchAlefHamza = false;
            object matchControl = false;
            object read_only = false;
            object visible = true;
            object replace = 2;
            object wrap = 1;

            wordApp.Selection.Find.Execute(ref ToFindText,
                ref matchCase, ref matchWholeWord,
                ref matchWildCards, ref matchSoundLike,
                ref nmatchAllforms, ref forward,
                ref wrap, ref format, ref replaceWithText,
                ref replace, ref matchKashida,
                ref matchDiactitics, ref matchAlefHamza,
                ref matchControl);
        }

        //Creeate the Doc Method
        static public bool CreateWordDocument(object filename, object SaveAs, string title, string date, string description, string purpose, string currBehavior)
        {
            Word.Application wordApp = new Word.Application();
            object missing = Missing.Value;
            Word.Document myWordDoc = null;

            if (File.Exists((string)filename))
            {
                object readOnly = false;
                object isVisible = false;
                wordApp.Visible = false;

                myWordDoc = wordApp.Documents.Open(ref filename, ref missing, ref readOnly,
                                        ref missing, ref missing, ref missing,
                                        ref missing, ref missing, ref missing,
                                        ref missing, ref missing, ref missing,
                                        ref missing, ref missing, ref missing, ref missing);
                myWordDoc.Activate();

                //find and replace
                FindAndReplace(wordApp, "<TITLE>", title);
                FindAndReplace(wordApp, "<DATE>", date);
                FindAndReplace(wordApp, "<DESCRIPTION>", description);
                FindAndReplace(wordApp, "<PURPOSE>", purpose);
                FindAndReplace(wordApp, "<CURRENTBEHAVIOR>", currBehavior);
            }
            else
            {
                //MessageBox.Show("File not Found!");
                return false;
            }

            //Save as
            myWordDoc.SaveAs2(ref SaveAs, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing,
                            ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing);

            myWordDoc.Close();
            wordApp.Quit();

            //MessageBox.Show("File Created!");
            return true;
        }


    }
}
