﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using Timer = System.Threading.Timer;

namespace FixedPartitionSimulation
{
    public partial class Form1 : Form
    {
        private int timer = 0;
        private const int kernel = 50;
        private int RAM, processes;
        private Random random = new Random();
        private bool timerBool = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (startTimer() == true)
            {
                timer++;
                label4.Text = Convert.ToString(timer);
                if (timer == 5)
                {
                    MessageBox.Show("Five seconds in");
                }
            }
           
        }
        private void setTimer(bool timerBool)
        {
            this.timerBool = timerBool;
        }
        private bool startTimer()
        {
            if (timerBool == true)
                return true;
            else
                return false;
           
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        // Proceed to Table Button
        private void button1_Click(object sender, EventArgs e)
        {
            if (memoryRAMBox.Text == "" || noProcessesBox.Text == "")
                MessageBox.Show("Fields cannot be empty", "Fixed Partition Simulator", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                int memoryRAM = 0, noProcesses = 0;
                try
                {
                    memoryRAM = Convert.ToInt32(memoryRAMBox.Text);
                    noProcesses = Convert.ToInt32(noProcessesBox.Text);
                    processes = noProcesses;
                    if (memoryRAM > 5000)
                        MessageBox.Show("RAM Size Exceeded Maximum RAM Requirement!", "Fixed Partition Simulator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else if (memoryRAM <= 0 || memoryRAM < 1000)
                        MessageBox.Show("RAM Size does not meet the minimum requirement", "Fixed Partition Simulator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else if (noProcesses > 10)
                        MessageBox.Show("Exceeded Maximum Processes Requirement!", "Fixed Partition Simulator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else if (noProcesses <= 0 || noProcesses < 5)
                        MessageBox.Show("No. of Processes does not meet the minimum requirement", "Fixed Partition Simulator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                    {
                        dataGridView1.Show();
                        fillTable(noProcesses);
                        configureMemoryPartitions(memoryRAM, noProcesses);
                        //storeMemoryKBinList();
                    }
                }
                catch(Exception)
                {
                    MessageBox.Show("Invalid Input", "Fixed Partition Simulator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
            }
        }

        // Allocate the Process ID's in the table
        private void fillTable(int processes)
        {
            for (int i = 0; i < processes; i++)
            {
                string processId = $"P{i + 1}"; // Generate process IDs (P1, P2, ...)
                dataGridView1.Rows.Add(processId); // Add a new row with the process ID
            }
        }
        double valueKB;
        // Store each Memory KB of each partitions in a List
        private void storeMemoryKBinList()
        {
           
        }

        // List to store added panels from For Loop below
        List<Panel> addedPanels = new List<Panel>();
        private void configureMemoryPartitions(int memoryRAM, int noProcesses)
        {
            memoryRAMPanel.Margin = new Padding(0); // Set zero margin for no spacing
            memoryRAMPanel.Padding = new Padding(0);
            double panelHeight = memoryRAMPanel.Height;
            double panelWidth = memoryRAMPanel.Width;
            double memoryUsable = memoryRAM - 50;

            int[] noPartitionsChoices = { noProcesses - 1, noProcesses, noProcesses - 2 };
            int randomIndex = random.Next(0, 3);
            int newNoProcesses = noPartitionsChoices[randomIndex];
            double heightOfPanels = panelHeight / newNoProcesses;
            decimal partitionSizes = (decimal)memoryUsable / (decimal)newNoProcesses;


            for (int i = 0; i < newNoProcesses; i++)
            {
                Panel panel = new Panel();
                panel.Size = new Size((int)panelWidth, (int)heightOfPanels);
                panel.BorderStyle = BorderStyle.FixedSingle;
                panel.Margin = new Padding(0);
                System.Windows.Forms.Label partitionLabel = new System.Windows.Forms.Label();
                partitionSizes = Math.Round(partitionSizes, 2);
                partitionLabel.Text = $"{partitionSizes.ToString()} KB";
                partitionLabel.AutoSize = true;
                panel.Controls.Add(partitionLabel);
                memoryRAMPanel.Controls.Add(panel);
                addedPanels.Add(panel); // every added panel is added to a List, so it can be tracked later
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridView1.Hide();
        }
        // Extract Data from Data Grid
        private void button3_Click(object sender, EventArgs e)
        {
            int rowCount = dataGridView1.RowCount;
            int columnCount = dataGridView1.ColumnCount;
            double[,] dataTable = new double[rowCount, columnCount];
           
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                DataGridViewRow row = dataGridView1.Rows[rowIndex];
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    if (columnIndex == 0)
                        dataTable[rowIndex, columnIndex] = rowIndex + 1;
                    else
                        dataTable[rowIndex, columnIndex] = Convert.ToDouble(row.Cells[columnIndex].Value.ToString());
                }
            }
            checkDataExtracted(dataTable);
            findShortestAllocationTime(dataTable);
        }

        // Find the shortest allocation time Process
        // Create an empty list to store the sequence of executed processes
        List<Process> executedProcesses = new List<Process>();
        private void findShortestAllocationTime(double[,] dataTable)
        {
            if (dataTable == null || dataTable.GetLength(0) <= 1 || dataTable.GetLength(1) < 3)
            {
                MessageBox.Show("Invalid data table: Empty, less than 2 rows, or less than 3 columns");
                return; // Handle invalid input gracefully
            }

            // Create a List to store Process objects with relevant data
            List<Process> processes = new List<Process>();
            for (int i = 0; i < dataTable.GetLength(0); i++)
            {
                processes.Add(new Process { ProcessId = dataTable[i, 0],MemoryRequirement = dataTable[i,1], AllocationTime = dataTable[i, 2],CompletionTime = dataTable[i,3] });
            }

            // Sort the processes based on allocation time (ascending order)
            processes.Sort((p1, p2) => p1.AllocationTime.CompareTo(p2.AllocationTime));


            // Iterate through sorted processes and add them to the sequence
            foreach (Process process in processes)
            {
                executedProcesses.Add(process);
                MessageBox.Show($"Shortest Allocation time is P{process.ProcessId} with an allocation time of {process.AllocationTime}"); // Display results progressively
            }

            // Display the sequence of executed processes
            MessageBox.Show("Sequence of executed processes:");
            foreach (Process process in executedProcesses)
            {
                MessageBox.Show(process.ToString()); // Use ToString() for formatted output
            }
            allocateProcess(executedProcesses, dataTable);


        }
        // Allocate Process to a Partition
        private void allocateProcess(List<Process> executedProcesses,double[,] dataTable)
        {
            if (executedProcesses == null || dataTable == null || addedPanels.Count == 0 || addedPanels.Any(p => !p.Controls.OfType<System.Windows.Forms.Label>().Any()))
            {
                // Handle invalid input: empty lists, null table, or missing labels in panels
                MessageBox.Show("Invalid input: Processes, data table, panels, or missing labels");
                return;
            }
            List<System.Windows.Forms.Label> labelList = new List<System.Windows.Forms.Label>();
            List<double> partitionMemorySizes = new List<double>();
            double valueKB = 0;
            foreach (Panel panels in addedPanels)
            {
                System.Windows.Forms.Label label = panels.Controls.OfType<System.Windows.Forms.Label>().FirstOrDefault();

                if (label != null)
                {
                    labelList.Add(label);
                    string origString = label.Text;
                    string extractedKB = origString.Substring(0, origString.IndexOf("KB"));

                    valueKB = Convert.ToDouble(extractedKB);

                    partitionMemorySizes.Add(valueKB);
                }

            }
            double panelHeight = addedPanels[0].Height;
            double panelWidth = addedPanels[0].Width;
            double fixedSizePartition = partitionMemorySizes[0];
            MessageBox.Show(fixedSizePartition.ToString());
            Thread thread = new Thread(() => Countdown(panelHeight, panelWidth,partitionMemorySizes, fixedSizePartition, labelList));
            thread.Start();
        }

        private void setLabelInThread(System.Windows.Forms.Label label, double timer)
        {
            // Update label text with Invoke
            if (label.InvokeRequired)
            {
                label.Invoke(new Action(() => label4.Text = timer.ToString()));
            }
            else
            {
                label.Text = timer.ToString(); // Update directly if on UI thread
            }
        }
        // Countdown
        private void Countdown( double panelHeight, double panelWidth, List<double> partitionMemorySizes, double fixedPartition, List<System.Windows.Forms.Label> labelList)
        {
           int processes = 0;
           int timer = 0;
           setLabelInThread(label4, timer);
            while (true)
            {
                foreach(Process process in executedProcesses)
                {
                    if(process.AllocationTime == timer)
                    {
                        paintPanel(process, panelHeight, panelWidth, partitionMemorySizes, fixedPartition, labelList);
                    }
                    if(returnCompletionTime(process) == timer)
                    {
                        unpaintPanel(process, panelHeight, panelWidth, partitionMemorySizes, fixedPartition, labelList);
                        processes++;
                    }
                    
                }
                if (processes == executedProcesses.Count)
                {
                    Thread.Sleep(5000);
                    MessageBox.Show("All Process Allocated!");
                    break; 
                }
                    
                else
                {
                    Thread.Sleep(2000);
                    timer++;
                    setLabelInThread(label4, timer);
                }
            }

        }

        private int returnCompletionTime(Process process)
        {
            int completionTime = (int)process.AllocationTime + (int)process.CompletionTime;
            return completionTime;
        }
        // Set Label of partitions
        private void setLabel(System.Windows.Forms.Label label, double partitionSize)
        {
            label.Text = partitionSize.ToString();
        }
        // Paint a panel that indicates a process being allocated in a memory partition
        private void paintPanel(Process process, double panelHeight, double panelWidth, List<double> partitionMemorySizes, double fixedPartition, List<System.Windows.Forms.Label> labelList)
        {
            int counter = 0;
            foreach(Panel panel in addedPanels)
            {

                if (partitionMemorySizes[counter] >= process.MemoryRequirement && partitionMemorySizes[counter] == fixedPartition)
                {
                    panel.Invoke(new Action(() =>
                    {
                        double percentage = Math.Round((process.MemoryRequirement / partitionMemorySizes[counter]), 2);
                        // Calculate Height Colored Section
                        double coloredHeight = panelHeight * percentage;

                        // Use Control.CreateGraphics() for drawing
                        using (Graphics g = panel.CreateGraphics())
                        {
                            g.FillRectangle(Brushes.Green, 0, 0, (int)panelWidth, (int)coloredHeight);
                        }
                        partitionMemorySizes[counter] -= process.MemoryRequirement;
                        labelList[counter].Text = $"{partitionMemorySizes[counter]}KB | P{process.ProcessId}";
                    }));                   
                    break;

                }
                else if(process.MemoryRequirement > fixedPartition)
                {
                    MessageBox.Show($"Process {process.ProcessId} is denied, too big");
                    break;
                }
                counter++;
            }

        }
        private void unpaintPanel(Process process, double panelHeight, double panelWidth, List<double> partitionMemorySizes, double fixedPartition, List<System.Windows.Forms.Label> labelList)
        {
            int counter = 0;
            foreach (Panel panel in addedPanels)
            {
                double unpaintPartition = fixedPartition -  process.MemoryRequirement;
                if (partitionMemorySizes[counter] == unpaintPartition)
                {
                    panel.Invoke(new Action(() =>
                    {
                        // Use Control.CreateGraphics() for drawing
                        using (Graphics g = panel.CreateGraphics())
                        {
                            g.FillRectangle(Brushes.Khaki, 0, 0, (int)panelWidth, (int)panelHeight);
                        }
                        partitionMemorySizes[counter] += process.MemoryRequirement;
                        labelList[counter].Text = $"{partitionMemorySizes[counter]}KB";
                    }));
                    break;
                }
                else
                    counter++; 

            }
        }
       
        // Reset Computer Button to Reconfigure new Partitions for the Memory(RAM)
        private void button2_Click(object sender, EventArgs e)
        {
            memoryRAMPanel.Controls.Clear();
            dataGridView1.Rows.Clear();
            memoryRAMBox.Clear();
            noProcessesBox.Clear();
        }

        private void checkDataExtracted(double[,] dataTable)
        {
            string message = "";

            for (int rowIndex = 0; rowIndex < dataTable.GetLength(0); rowIndex++)
            {
                // Create a string for the current row
                string rowString = "";
                for (int columnIndex = 0; columnIndex < dataTable.GetLength(1); columnIndex++)
                {
                    rowString += dataTable[rowIndex, columnIndex] + " ";
                }

                // Remove the trailing space from the row string
                rowString = rowString.TrimEnd();

                // Add the row string to the message with a newline
                message += rowString + "\n";
            }

            // Remove the trailing newline from the message (optional)
            message = message.TrimEnd('\n');

            // Display the message in a MessageBox
            MessageBox.Show(message, "Data Grid Content");
        }

    }
    // Class to represent a process with its ID and allocation time
    public class Process
    {
        public double ProcessId { get; set; }
        public double AllocationTime { get; set; }
        public double MemoryRequirement { get; set; }
        public double CompletionTime { get; set; }

        public override string ToString() // Optional for formatted output
        {
            return $"Process: P{ProcessId}, Allocation Time: {AllocationTime}, Memory Requirement: {MemoryRequirement}, Completion Time: {CompletionTime}";
        }

    }
}

