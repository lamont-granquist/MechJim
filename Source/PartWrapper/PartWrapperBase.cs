namespace MechJim.PartWrapper {
    public class PartWrapperBase {
        public Part part { get; set; }
        public bool mark { get; set; }

        public PartWrapperBase(Part part) {
            this.part = part;
        }

    }
}
