@Component
export default class GaugeCard extends Vue {
    @Prop({type: String, default:"fas fa-server"}) icon
    @Prop({ type: String, default: "#418BCA" }) background
    @Prop({ type: String, default: "white" }) color
    @Prop({ type: String | Number, default: 123 }) title
    @Prop({ type: String, default: "nodes" }) units
    @Prop({ type: String, default: "100%" }) width
}
