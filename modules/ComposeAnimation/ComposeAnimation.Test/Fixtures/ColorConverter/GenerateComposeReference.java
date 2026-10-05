import java.lang.reflect.*;
import java.util.*;
import androidx.compose.animation.ColorVectorConverterKt;
import androidx.compose.animation.core.*;
import androidx.compose.ui.graphics.Color;
import androidx.compose.ui.graphics.ColorKt;
import androidx.compose.ui.graphics.colorspace.*;

// Invoke the fixed Compose jars, not a copy of the conversion algorithm.
public class GenerateComposeReference {
    static final TwoWayConverter<Color, AnimationVector4D> CONVERTER =
        ColorVectorConverterKt.getVectorConverter(Color.Companion).invoke(ColorSpaces.INSTANCE.getSrgb());
    static Color color(int argb) throws Exception {
        return (Color)Color.class.getMethod("box-impl",long.class).invoke(null,ColorKt.Color(argb));
    }
    static String argb(Color c) throws Exception {
        long packed=(long)Color.class.getMethod("unbox-impl").invoke(c);
        int argb=(int)ColorKt.class.getMethod("toArgb-8_81llA",long.class).invoke(null,packed);
        return String.format(Locale.ROOT,"%08X",argb);
    }
    static void to(int argb) throws Exception {
        AnimationVector4D v=CONVERTER.getConvertToVector().invoke(color(argb));
        System.out.printf(Locale.ROOT,"{\"kind\":\"to\",\"argb\":\"%08X\",\"vector\":%s,\"output\":\"%s\"}%n",
            argb,Arrays.toString(new float[]{v.getV1(),v.getV2(),v.getV3(),v.getV4()}),argb(CONVERTER.getConvertFromVector().invoke(v)));
    }
    static void from(float alpha,float l,float a,float b) throws Exception {
        AnimationVector4D v=new AnimationVector4D(alpha,l,a,b);
        System.out.printf(Locale.ROOT,"{\"kind\":\"from\",\"vector\":%s,\"output\":\"%s\"}%n",
            Arrays.toString(new float[]{alpha,l,a,b}),argb(CONVERTER.getConvertFromVector().invoke(v)));
    }
    public static void main(String[] args) throws Exception {
        System.out.println("{\"kind\":\"baseline\",\"commit\":\"a9e47e8aa04c95cfe572ab4297c770f3cbfb234e\",\"compose\":\"1.13.0-alpha01\"}");
        for(int c:new int[]{0,0x00FF0000,0xFF000000,0xFFFFFFFF,0xFFFF0000,0xFF00FF00,0xFF0000FF,0xFF49454F,0xFF1D192B,0xFF126E60,0x7F123456}) to(c);
        int[] grid={0,1,16,64,128,192,254,255};
        for(int r:grid) for(int g:grid) for(int b:grid) to(0xFF000000|(r<<16)|(g<<8)|b);
        for(int alpha=0;alpha<256;alpha++) to((alpha<<24)|0x126E60);
        Random random=new Random(0xC010A);
        for(int i=0;i<1024;i++) to(random.nextInt());
        for(int i=0;i<512;i++) from(random.nextFloat()*1.2f-.1f,random.nextFloat()*1.2f-.1f,random.nextFloat()*1.2f-.6f,random.nextFloat()*1.2f-.6f);
        from(2f,2f,1f,-1f); from(-1f,-1f,-1f,1f);
        from(1f,.2983765f,.012129557f,-.024593044f);
        from(1f,.2984362f,.01214002f,-.02456663f);
        // Half midpoint, subnormal and signed-zero boundaries, combined with chromatic channels.
        for(float l:new float[]{.500244140625f,Math.nextDown(.500244140625f),Math.nextUp(.500244140625f),0f,Float.intBitsToFloat(1),0.0000000298023223876953125f,.00006103515625f})
            for(float a:new float[]{.12506103515625f,-.12506103515625f,0f,-0f}) from(.5f,l,a,-.02f);
        for(int i=0;i<256;i++) from((i+.5f)/255f,.5f,0f,0f);
        int[][] pairs={{0xFF49454F,0xFF1D192B},{0xFF1D192B,0xFF49454F},{0xFF126E60,0xFF365A80},
            {0xFF000000,0xFFFFFFFF},{0xFFFF0000,0xFF0000FF},{0x00126E60,0xFF126E60}};
        for(int[] pair:pairs) for(float[] spec:new float[][]{{1f,1600f},{.6f,800f},{1.4f,1400f}}) {
            TargetBasedAnimation<Color,AnimationVector4D> animation=new TargetBasedAnimation<>(
                new SpringSpec<Color>(spec[0],spec[1],null),CONVERTER,color(pair[0]),color(pair[1]),new AnimationVector4D(0f,0f,0f,0f));
            for(long time:new long[]{0,1,16,32,48,49,64,80,96,160,400,800})
                System.out.printf(Locale.ROOT,"{\"kind\":\"animation\",\"initial\":\"%08X\",\"target\":\"%08X\",\"damping\":%s,\"stiffness\":%s,\"timeMillis\":%d,\"output\":\"%s\"}%n",
                    pair[0],pair[1],spec[0],spec[1],time,argb(animation.getValueFromNanos(time*1000000L)));
        }
    }
}
