﻿namespace SuperBMD.Materials
{
    public class IndirectTexturing
    {
        /// <summary>
        /// Determines if an indirect texture lookup is to take place
        /// </summary>
        public bool HasLookup { get; set; } = false;
        /// <summary>
        /// The number of indirect texturing stages to use
        /// </summary>
        public byte IndTexStageNum { get; set; } = 0;

        public IndirectTevOrder[] TevOrders { get; set; } = new IndirectTevOrder[4];

        /// <summary>
        /// The dynamic 2x3 matrices to use when transforming the texture coordinates
        /// </summary>
        public IndirectTexMatrix[] Matrices { get; set; } = new IndirectTexMatrix[3];
        /// <summary>
        /// U and V scales to use when transforming the texture coordinates
        /// </summary>
        public IndirectTexScale[] Scales { get; set; } = new IndirectTexScale[4];
        /// <summary>
        /// Instructions for setting up the specified TEV stage for lookup operations
        /// </summary>
        public IndirectTevStage[] TevStages { get; set; } = new IndirectTevStage[16];


        public IndirectTexturing()
        {
            for (int i = 0; i < 4; i++)
                TevOrders[i] = new IndirectTevOrder(TexCoordId.Null, TexMapId.Null);

            for (int i = 0; i < 3; i++)
                Matrices[i] = new IndirectTexMatrix(new Matrix2x3(0.5f, 0.0f, 0.0f, 0.0f, 0.5f, 0.0f), 1);

            for (int i = 0; i < 4; i++)
                Scales[i] = new IndirectTexScale(IndirectScale.ITS_1, IndirectScale.ITS_1);

            for (int i = 0; i < 3; i++)
                TevStages[i] = new IndirectTevStage(
                    TevStageId.TevStage0,
                    IndirectFormat.ITF_8,
                    IndirectBias.ITB_S,
                    IndirectMatrix.ITM_OFF,
                    IndirectWrap.ITW_OFF,
                    IndirectWrap.ITW_OFF,
                    false,
                    false,
                    IndirectAlpha.ITBA_OFF
                    );
        }

        public IndirectTexturing(ref EndianBinaryReader reader)
        {
            HasLookup = reader.ReadBool();
            IndTexStageNum = reader.ReadByte();
            reader.Skip(2);

            TevOrders = new IndirectTevOrder[4];
            for (int i = 0; i < 4; i++)
                TevOrders[i] = new IndirectTevOrder(ref reader);

            Matrices = new IndirectTexMatrix[3];
            for (int i = 0; i < 3; i++)
                Matrices[i] = new IndirectTexMatrix(ref reader);

            Scales = new IndirectTexScale[4];
            for (int i = 0; i < 4; i++)
                Scales[i] = new IndirectTexScale(ref reader);

            TevStages = new IndirectTevStage[16];
            for (int i = 0; i < 16; i++)
                TevStages[i] = new IndirectTevStage(ref reader);
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            writer.Write(HasLookup);
            writer.Write(IndTexStageNum);

            writer.Write((short)-1);

            for (int i = 0; i < 4; i++)
                TevOrders[i].Write(ref writer);

            for (int i = 0; i < 3; i++)
                Matrices[i].Write(ref writer);

            for (int i = 0; i < 4; i++)
                Scales[i].Write(ref writer);

            for (int i = 0; i < 16; i++)
                TevStages[i].Write(ref writer);
        }


    }
}
