void ComplexLogUV_float(
    float2 UV,
    out float2 Out
){

    float epsilon = 1e-6 ;

    float r = max(length(UV), epsilon);
    float theta = atan2(UV.y, UV.x);

    float u = log(r);
    float v = theta / TWO_PI;

    Out = float2(u, v);


}

void CenterUV_float(
    float2 UV,
    out float2 Out)
    {
        Out = UV * 2.0 - 1.0;
    }



void ComplexExpUV_float(
    float2 UV,
    out float2 Out
){
    float r = exp(UV.x);
    float theta = UV.y * TWO_PI;

    Out = r * float2(cos(theta), sin(theta));
}