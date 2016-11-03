using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using System.Data.SqlClient;
using System.Collections;

namespace KalleKonv01
{
    class Program
    {
        //HouseKeeping
        static string NyKalleSQL;

        static SqlConnection OldKalleSQLConnection = new SqlConnection("data source=LAPTOP-016\\SQLexpress;initial catalog=OldKalle;persist security info=True;user id=sa;password=10Xblank");
        static SqlConnection NyKalleSQLConnection = new SqlConnection("data source=LAPTOP-016\\SQLexpress;initial catalog=NyKalle;persist security info=True;user id=sa;password=10Xblank");

       
        // ********************************************
        //Åbning af databaser
        //Der skal åbnes to database connections
        //En til OldKalle og en til NyKalle
        // ********************************************
        static OldKalleEntities OldKalleDb = new OldKalleEntities();
        static NyKalleEntities NyKalleDb = new NyKalleEntities();

        static void Main(string[] args)
        {
            // ******************************************************************
            // For at kunne fodre med SQL statements skal det gøres på denne måde
            // ******************************************************************
            OldKalleSQLConnection.Open();
            NyKalleSQLConnection.Open();

            // ****************************
            // Slet sammenhænge i databasen
            // ****************************
            DeleteConstraint();

            // *********************************************************
            // Slet og opret tabellerne: tblFrame, tblSpil og tblSpiller
            // *********************************************************
            DeletetblFrame();
            DeletetblSpil();
            DeletetblSpiller();

            // *******************************
            // Opret database sammenhænge igen
            // *******************************
            AddConstraint();

            // **************************************************
            // Så lukker vi for boks 1 for SQL statements syntax
            // **************************************************
            OldKalleSQLConnection.Close();
            NyKalleSQLConnection.Close();
            

            // *********************************************************************************
            // Jeg skal have noget logik at pakke rundt om indlæsning og oprettelse af NyKalleDB
            // Du har spillerId: min 1 - max 88.
            // Men ikke alle er der mere
            // *********************************************************************************

            for (int i = 1; i <= 88; i++)
            {
                Console.WriteLine("Skal hente en spiller: " + i);

                List<Stam> OldStam = OldKalleDb.Stam.Where(t => t.Stam_Auto1 == i).ToList();

                if (OldStam.Count == 0)
                {
                    Console.WriteLine("Spiller findes ikke: " + i);
                    continue;
                }
                Console.WriteLine("Spiller: " + i + ", Hedder: " + OldStam[0].Stam_Navn);

                string spillerForNavn = string.Empty;
                string spillerEfterNavn = string.Empty;
                int antalBlanke = 0;

                foreach (char c in OldStam[0].Stam_Navn)
                {
                    if (c == ' ')
                    {
                        antalBlanke = antalBlanke + 1;
                    }
                }
                
                // **********************************************
                // Nu skal der splittes ud i fornavn og efternavn
                // **********************************************
                if (antalBlanke == 0)
                {
                    spillerForNavn = OldStam[0].Stam_Navn;
                    spillerEfterNavn = "─";
                }
                else
                {
                    spillerForNavn = OldStam[0].Stam_Navn.Substring(0, OldStam[0].Stam_Navn.IndexOf(" "));
                    spillerEfterNavn = OldStam[0].Stam_Navn.Substring(OldStam[0].Stam_Navn.IndexOf(" ") +1);
                }

                //Console.WriteLine("spillerFornavn: " + spillerForNavn + " spillerEfterNavn: " + spillerEfterNavn + " antalBlanke: " + antalBlanke);

                // ***************************************************
                // Så skal spilleren oprettes i den nye database
                // ***************************************************
                tblSpiller NySpiller = new tblSpiller();

              
                NySpiller.CN_SpillerForNavn = spillerForNavn;
                NySpiller.CN_SpillerEfterNavn = spillerEfterNavn;
                NySpiller.CN_SpillerInit = OldStam[0].Stam_init;
                NySpiller.CN_SpillerOptDato = OldStam[0].Stam_Opt_dato;
                NySpiller.CN_SpillerUdMeldDato = OldStam[0].Stam_Udmeldt_dato;
                NySpiller.CN_SpillerFormand = OldStam[0].Stam_Formand;
                NySpiller.CN_SpillerNastFormand = OldStam[0].Stam_Nastformand;
                NySpiller.CN_SpillerKasser = OldStam[0].Stam_Kasser;
                NySpiller.CN_SpillerRevisor = OldStam[0].Stam_Revisor;
                NySpiller.CN_SpillerSkemaAnsv = OldStam[0].Stam_Skema_ansvarlig;
                NySpiller.CN_SpillerWebAnsv = OldStam[0].Stam_Web;
                NySpiller.CN_EMail = OldStam[0].Stam_E_Mail;
                NySpiller.CN_AktivMedl = OldStam[0].Stam_Aktiv_medlem;
                NySpiller.CN_PassivMedl = OldStam[0].Stam_Passiv;
                NySpiller.CN_ProveMedl = OldStam[0].Stam_Provemedlem;

                try
                {
                    NyKalleDb.tblSpiller.Add(NySpiller);
                    NyKalleDb.SaveChanges();
                }
                catch
                {
                    Console.WriteLine("UPS noget gik helt galt ved oprettelsen af en spiller");
                    Console.ReadLine();
                }


                // *************************************************
                // Så skal vi have læst den fundne spillers spil ind
                // *************************************************
                // Console.WriteLine("Skal hente en spillers spil: " + i);

                List<Score> OldScore = OldKalleDb.Score.OrderBy(y => y.Sesson).ThenBy(y => y.Auto2).Where(t => t.Auto1 == i).ToList();

                // ********************************************************
                // Loop til oprettelse af tblSpil og tblFrame i ny database
                // ********************************************************
                foreach (var item in OldScore)
                {
                    decimal ScoreSum = 0.000M; 
                    decimal ScoreAntSpil = 0.000M;
                    decimal ScoreSnit = 0.000M;

                    //Console.WriteLine("Score Auto1: " + item.Auto1 + " ´Sesson: " + item.Sesson + " Score Auto2: " + item.Auto2 + " Dato for spil: " + item.Dato);

                    // ******************************************************
                    // Find de spil der er blevet spillet på den enkelte dag.
                    // ******************************************************
                    if(item.Spil1_spillet == 1)
                    {
                        ScoreSum = ScoreSum + (decimal)(item.Spil1);
                        ScoreAntSpil = ScoreAntSpil + 1;

                        //Console.WriteLine("Spilleren har spillet spil 1: " + item.Spil1_spillet + " Scoret: " + item.Spil1 + " ScoreSum: " + ScoreSum + " ScoreAntSpil: " + ScoreAntSpil);
                    }

                    if (item.Spil2_spillet == 1)
                    {
                        ScoreSum = ScoreSum + (decimal)(item.Spil2);
                        ScoreAntSpil = ScoreAntSpil + 1;

                        //Console.WriteLine("Spilleren har spillet spil 2: " + item.Spil2_spillet + " Scoret: " + item.Spil2 + " ScoreSum: " + ScoreSum + " ScoreAntSpil: " + ScoreAntSpil);
                    }

                    if (item.Spil3_spillet == 1)
                    {
                        ScoreSum = ScoreSum + (decimal)(item.Spil3);
                        ScoreAntSpil = ScoreAntSpil + 1;

                        //Console.WriteLine("Spilleren har spillet spil 3: " + item.Spil3_spillet + " Scoret: " + item.Spil3 + " ScoreSum: " + ScoreSum + " ScoreAntSpil: " + ScoreAntSpil);
                    }

                    if (item.Spil4_spillet == 1)
                    {
                        ScoreSum = ScoreSum + (decimal)(item.Spil4);
                        ScoreAntSpil = ScoreAntSpil + 1;

                        //Console.WriteLine("Spilleren har spillet spil 4: " + item.Spil4_spillet + " Scoret: " + item.Spil4 + " ScoreSum: " + ScoreSum + " ScoreAntSpil: " + ScoreAntSpil);
                    }

                    if (item.Spil5_spillet == 1)
                    {
                        ScoreSum = ScoreSum + (decimal)(item.Spil5);
                        ScoreAntSpil = ScoreAntSpil + 1;

                        // Console.WriteLine("Spilleren har spillet spil 5: " + item.Spil5_spillet + " Scoret: " + item.Spil5 + " ScoreSum: " + ScoreSum + " ScoreAntSpil: " + ScoreAntSpil);
                    }

                    if (ScoreAntSpil == 0.000M)
                    {
                        Console.WriteLine("Det her går grumme galt. Der er et spil for an aften, hvor der ikke er spil i: " +
                            "Score Auto1: " + item.Auto1 + " ´Sesson: " + item.Sesson + " Score Auto2: " + item.Auto2 + " Dato for spil: " + item.Dato);
                        Console.ReadLine();
                    }

                    ScoreSnit = ScoreSum / ScoreAntSpil;

                    //Console.WriteLine("Gennemsnit for spillerdagen er: " + Math.Round(ScoreSnit,2) +  "   Og der er spillet: " + ScoreAntSpil +" Spil");

                    // *************************************
                    // Oprettelse af et spil for spilledagen
                    // *************************************

                    tblSpil NySpil = new tblSpil();

                    NySpil.CN_SpilSpillerPK = NySpiller.CN_SpillerPK;
                    NySpil.CN_SpilDato = (DateTime)item.Dato;
                    NySpil.CN_SpilSesson = (int)item.Sesson;
                    NySpil.CN_SpilKalleKamel = item.Kamel_passer;
                    NySpil.CN_SpilPlacering = (int)item.Placering;
                    NySpil.CN_SpilGennemsnit = ScoreSnit;

                    try
                    {
                        NyKalleDb.tblSpil.Add(NySpil);
                        NyKalleDb.SaveChanges();
                    }
                    catch
                    {
                        Console.WriteLine("UPS noget gik helt galt ved oprettelsen af et spil");
                        Console.ReadLine();
                    }

                    // *****************************************************
                    // Oprettelse af scoren i det enkelte spil på spiledagen
                    // *****************************************************
                    if (item.Spil1_spillet == 1)
                    {
                        tblFrame NyFrame = new tblFrame();

                        NyFrame.CN_FrameSpilPK = NySpil.CN_SpilPK;
                        NyFrame.CN_FrameNr = 1;
                        NyFrame.CN_FrameScore = (int)item.Spil1;

                        try
                        {
                            NyKalleDb.tblFrame.Add(NyFrame);
                            NyKalleDb.SaveChanges();
                        }
                        catch
                        {
                            Console.WriteLine("UPS noget gik helt galt ved oprettelsen af en frame");
                            Console.ReadLine();
                        }
                    }

                    if (item.Spil2_spillet == 1)
                    {
                        tblFrame NyFrame = new tblFrame();

                        NyFrame.CN_FrameSpilPK = NySpil.CN_SpilPK;
                        NyFrame.CN_FrameNr = 2;
                        NyFrame.CN_FrameScore = (int)item.Spil2;

                        try
                        {
                            NyKalleDb.tblFrame.Add(NyFrame);
                            NyKalleDb.SaveChanges();
                        }
                        catch
                        {
                            Console.WriteLine("UPS noget gik helt galt ved oprettelsen af en frame");
                            Console.ReadLine();
                        }
                    }

                    if (item.Spil3_spillet == 1)
                    {
                        tblFrame NyFrame = new tblFrame();

                        NyFrame.CN_FrameSpilPK = NySpil.CN_SpilPK;
                        NyFrame.CN_FrameNr = 3;
                        NyFrame.CN_FrameScore = (int)item.Spil3;

                        try
                        {
                            NyKalleDb.tblFrame.Add(NyFrame);
                            NyKalleDb.SaveChanges();
                        }
                        catch
                        {
                            Console.WriteLine("UPS noget gik helt galt ved oprettelsen af en frame");
                            Console.ReadLine();
                        }
                    }

                    if (item.Spil4_spillet == 1)
                    {
                        tblFrame NyFrame = new tblFrame();

                        NyFrame.CN_FrameSpilPK = NySpil.CN_SpilPK;
                        NyFrame.CN_FrameNr = 4;
                        NyFrame.CN_FrameScore = (int)item.Spil4;

                        try
                        {
                            NyKalleDb.tblFrame.Add(NyFrame);
                            NyKalleDb.SaveChanges();
                        }
                        catch
                        {
                            Console.WriteLine("UPS noget gik helt galt ved oprettelsen af en frame");
                            Console.ReadLine();
                        }
                    }

                    if (item.Spil5_spillet == 1)
                    {
                        tblFrame NyFrame = new tblFrame();

                        NyFrame.CN_FrameSpilPK = NySpil.CN_SpilPK;
                        NyFrame.CN_FrameNr = 5;
                        NyFrame.CN_FrameScore = (int)item.Spil5;

                        try
                        {
                            NyKalleDb.tblFrame.Add(NyFrame);
                            NyKalleDb.SaveChanges();
                        }
                        catch
                        {
                            Console.WriteLine("UPS noget gik helt galt ved oprettelsen af en frame");
                            Console.ReadLine();
                        }
                    }
                }
            }

            Console.WriteLine("Det var alt for denne gang, så langt så godt");
            Console.ReadLine();

        }
        // Her slutter Main()


        // ****************************************************
        // Slet alle CONSTRAINT bindinger i databasen
        // ****************************************************
        private static void DeleteConstraint()
            {
            // tblFrame
                try
                {
                    NyKalleSQL = "ALTER TABLE[dbo].[tblFrame] DROP CONSTRAINT[FK_tblFrame_tblSpil]";
                    SqlCommand objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                    objCmd.ExecuteNonQuery();
                }
                catch (System.Exception e) {
                    Console.WriteLine(e.Message);
                    // return;
                }

                try {
                    NyKalleSQL = "ALTER TABLE[dbo].[tblFrame] DROP CONSTRAINT[DF_tblFrame_CN_FrameScore]";
                    SqlCommand objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                    objCmd.ExecuteNonQuery();
                }
                catch (System.Exception e) {
                    Console.WriteLine(e.Message);
                    // return;
                }

            // tblSpil
                try { 
                    NyKalleSQL = "ALTER TABLE [dbo].[tblSpil] DROP CONSTRAINT [FK_tblSpil_tblSpiller]";
                    SqlCommand objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                    objCmd.ExecuteNonQuery();
                }
                catch (System.Exception e) {
                    Console.WriteLine(e.Message);
                    // return;
                }
                try { 
                    NyKalleSQL = "ALTER TABLE [dbo].[tblSpil] DROP CONSTRAINT [DF_tblSpil_CN_SpilGennemsnit]";
                    SqlCommand objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                    objCmd.ExecuteNonQuery();
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e.Message);
                    // return;
                }
                try {
                    NyKalleSQL = "ALTER TABLE [dbo].[tblSpil] DROP CONSTRAINT [DF_tblSpil_CN_SpilPlacering]";
                    SqlCommand objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                    objCmd.ExecuteNonQuery();
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e.Message);
                    // return;
                }

                Console.WriteLine("Constraint er slettet");

                return;
            }

        private static void DeletetblFrame()
        { 
            // **************************************************
            // Slet tblFrame tabellen i NyKalle og opret den igen
            // **************************************************
            try
            {

                //NyKalleSQL = "ALTER TABLE[dbo].[tblFrame] DROP CONSTRAINT[FK_tblFrame_tblSpil]";
                //SqlCommand objCmd = new SqlCommand(NyKalleSQL, NyKalleCon);
                //objCmd.ExecuteNonQuery();

                //NyKalleSQL = "ALTER TABLE[dbo].[tblFrame] DROP CONSTRAINT[DF_tblFrame_CN_FrameScore]";
                //objCmd = new SqlCommand(NyKalleSQL, NyKalleCon);
                //objCmd.ExecuteNonQuery();

                NyKalleSQL = "DROP TABLE[dbo].[tblFrame]";
                SqlCommand objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                NyKalleSQL = "SET ANSI_NULLS ON";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                NyKalleSQL = "SET QUOTED_IDENTIFIER ON";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                NyKalleSQL = "CREATE TABLE[dbo].[tblFrame](" +
                             "[CN_FramePK][int] IDENTITY(1, 1) NOT NULL," +
                             "[CN_FrameSpilPK] [int] NOT NULL," +
                             "[CN_FrameNr] [int] NOT NULL," +
                             "[CN_FrameScore] [int] NOT NULL," +
                             "CONSTRAINT[PK_tblFrame] PRIMARY KEY CLUSTERED" +
                             "(" +
                             "[CN_FramePK] ASC" +
                             ")WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]" +
                             ") ON[PRIMARY]";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                //NyKalleSQL = "ALTER TABLE[dbo].[tblFrame] ADD CONSTRAINT[DF_tblFrame_CN_FrameScore]  DEFAULT((0)) FOR[CN_FrameScore]";
                //objCmd = new SqlCommand(NyKalleSQL, NyKalleCon);
                //objCmd.ExecuteNonQuery();

                //NyKalleSQL = "ALTER TABLE[dbo].[tblFrame] WITH CHECK ADD CONSTRAINT[FK_tblFrame_tblSpil] FOREIGN KEY([CN_FrameSpilPK])REFERENCES[dbo].[tblSpil] ([CN_SpilPK])";
                //objCmd = new SqlCommand(NyKalleSQL, NyKalleCon);
                //objCmd.ExecuteNonQuery();

                //NyKalleSQL = "ALTER TABLE[dbo].[tblFrame] CHECK CONSTRAINT[FK_tblFrame_tblSpil]";
                //objCmd = new SqlCommand(NyKalleSQL, NyKalleCon);
                //objCmd.ExecuteNonQuery();
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            Console.WriteLine("tblFrame tabellen er nu nulstillet");
        }

        private static void DeletetblSpil()
        { 
            // **************************************************
            // Slet tblSpil tabellen i NyKalle, og opret den igen
            // **************************************************
            try
            {

                //NyKalleSQL = "ALTER TABLE [dbo].[tblSpil] DROP CONSTRAINT [FK_tblSpil_tblSpiller]";
                //SqlCommand objCmd = new SqlCommand(NyKalleSQL, NyKalleCon);
                //objCmd.ExecuteNonQuery();

                //NyKalleSQL = "ALTER TABLE [dbo].[tblSpil] DROP CONSTRAINT [DF_tblSpil_CN_SpilGennemsnit]";
                //objCmd = new SqlCommand(NyKalleSQL, NyKalleCon);
                //objCmd.ExecuteNonQuery();

                //NyKalleSQL = "ALTER TABLE [dbo].[tblSpil] DROP CONSTRAINT [DF_tblSpil_CN_SpilPlacering]";
                //objCmd = new SqlCommand(NyKalleSQL, NyKalleCon);
                //objCmd.ExecuteNonQuery();

                NyKalleSQL = "DROP TABLE [dbo].[tblSpil]";
                SqlCommand objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                NyKalleSQL = "SET ANSI_NULLS ON";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                NyKalleSQL = "SET QUOTED_IDENTIFIER ON";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                NyKalleSQL = "CREATE TABLE[dbo].[tblSpil](" +
                             "[CN_SpilPK] [int] IDENTITY(1,1) NOT NULL," +
                             "[CN_SpilSpillerPK] [int] NOT NULL," +
                             "[CN_SpilDato] [date] NOT NULL," +
                             "[CN_SpilSesson] [int] NOT NULL," +
                             "[CN_SpilKalleKamel] [bit] NULL," +
                             "[CN_SpilPlacering] [int] NOT NULL," +
                             "[CN_SpilGennemsnit] [decimal](6, 2) NOT NULL," +
                             "CONSTRAINT[PK_tblSpil] PRIMARY KEY CLUSTERED" +
                             "(" +
                             "[CN_SpilPK] ASC" +
                             ")WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]" +
                             ") ON[PRIMARY]";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                //NyKalleSQL = "ALTER TABLE [dbo].[tblSpil] ADD  CONSTRAINT [DF_tblSpil_CN_SpilPlacering]  DEFAULT ((0)) FOR [CN_SpilPlacering]";
                //objCmd = new SqlCommand(NyKalleSQL, NyKalleCon);
                //objCmd.ExecuteNonQuery();

                //NyKalleSQL = "ALTER TABLE [dbo].[tblSpil] ADD  CONSTRAINT [DF_tblSpil_CN_SpilGennemsnit]  DEFAULT ((0)) FOR [CN_SpilGennemsnit]";
                //objCmd = new SqlCommand(NyKalleSQL, NyKalleCon);
                //objCmd.ExecuteNonQuery();

                //NyKalleSQL = "ALTER TABLE [dbo].[tblSpil] WITH CHECK ADD CONSTRAINT [FK_tblSpil_tblSpiller] FOREIGN KEY([CN_SpilSpillerPK]) REFERENCES[dbo].[tblSpiller]([CN_SpillerPK])";
                //objCmd = new SqlCommand(NyKalleSQL, NyKalleCon);
                //objCmd.ExecuteNonQuery();

                //NyKalleSQL = "ALTER TABLE [dbo].[tblSpil] CHECK CONSTRAINT [FK_tblSpil_tblSpiller]";
                //objCmd = new SqlCommand(NyKalleSQL, NyKalleCon);
                //objCmd.ExecuteNonQuery();
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            Console.WriteLine("tblSpil tabellen er nu nulstillet");
        }


        private static void DeletetblSpiller()
        { 
            // *****************************************************
            // Slet tblSpiller tabellen i NyKalle, og opret den igen
            // *****************************************************
            try
            {

                NyKalleSQL = "DROP TABLE [dbo].[tblSpiller]";
                SqlCommand objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                NyKalleSQL = "SET ANSI_NULLS ON";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                NyKalleSQL = "SET QUOTED_IDENTIFIER ON";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                NyKalleSQL = "CREATE TABLE[dbo].[tblSpiller](" +
                             "[CN_SpillerPK][int] IDENTITY(1, 1) NOT NULL," +
                             "[CN_SpillerForNavn] [nvarchar](50) NOT NULL," +
                             "[CN_SpillerEfterNavn] [nvarchar](50) NULL," +
                             "[CN_SpillerInit] [nvarchar](10) NULL," +
                             "[CN_SpillerOptDato] [date] NULL," +
                             "[CN_SpillerUdMeldDato] [date] NULL," +
                             "[CN_SpillerFormand] [bit] NULL," +
                             "[CN_SpillerNastFormand] [bit] NULL," +
                             "[CN_SpillerKasser] [bit] NULL," +
                             "[CN_SpillerRevisor] [bit] NULL," +
                             "[CN_SpillerSkemaAnsv] [bit] NULL," +
                             "[CN_SpillerWebAnsv] [bit] NULL," +
                             "[CN_EMail] [nvarchar](50) NULL," +
                             "[CN_AktivMedl] [bit] NULL," +
                             "[CN_PassivMedl] [bit] NULL," +
                             "[CN_ProveMedl] [bit] NULL," +
                             "CONSTRAINT[PK_tblSpiller] PRIMARY KEY CLUSTERED" +
                             "(" +
                             "[CN_SpillerPK] ASC" +
                             ")WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]" +
                             ") ON[PRIMARY]";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            Console.WriteLine("tblSpiller tabellen er nu nulstillet");
        }

        private static void AddConstraint()
        { 
            // ******************************
            // Opret Constraint på tabellerne
            // ******************************
            try
            {
                
                // tblFrame
                NyKalleSQL = "ALTER TABLE[dbo].[tblFrame] ADD CONSTRAINT[DF_tblFrame_CN_FrameScore]  DEFAULT((0)) FOR[CN_FrameScore]";
                SqlCommand objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                NyKalleSQL = "ALTER TABLE[dbo].[tblFrame] WITH CHECK ADD CONSTRAINT[FK_tblFrame_tblSpil] FOREIGN KEY([CN_FrameSpilPK])REFERENCES[dbo].[tblSpil]([CN_SpilPK])";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                NyKalleSQL = "ALTER TABLE[dbo].[tblFrame] CHECK CONSTRAINT[FK_tblFrame_tblSpil]";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                // tblSpil
                NyKalleSQL = "ALTER TABLE [dbo].[tblSpil] ADD  CONSTRAINT[DF_tblSpil_CN_SpilPlacering]  DEFAULT ((0)) FOR [CN_SpilPlacering]";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                NyKalleSQL = "ALTER TABLE [dbo].[tblSpil] ADD  CONSTRAINT[DF_tblSpil_CN_SpilGennemsnit]  DEFAULT ((0)) FOR [CN_SpilGennemsnit]";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                NyKalleSQL = "ALTER TABLE [dbo].[tblSpil] WITH CHECK ADD CONSTRAINT[FK_tblSpil_tblSpiller] FOREIGN KEY([CN_SpilSpillerPK]) REFERENCES[dbo].[tblSpiller]([CN_SpillerPK])";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();

                NyKalleSQL = "ALTER TABLE [dbo].[tblSpil] CHECK CONSTRAINT[FK_tblSpil_tblSpiller]";
                objCmd = new SqlCommand(NyKalleSQL, NyKalleSQLConnection);
                objCmd.ExecuteNonQuery();
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            Console.WriteLine("Constraint oprettet igen");

        }
        
   }
}
