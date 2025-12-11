import pandas as pd
import matplotlib.pyplot as plt

# Leer CSV con separador ;
df = pd.read_csv("timings.csv", sep=";")

# Cores y tiempos
cores = df["cores"]
times = df["time"]

# Calcular speed-up: S(p) = T(1) / T(p)
T1 = times.iloc[0]
speedup = T1 / times

# Crear gráfica
plt.figure(figsize=(8, 5))
plt.plot(cores, speedup, marker="o")
plt.title("Speed-up vs Number of Cores")
plt.xlabel("Cores")
plt.ylabel("Speed-up")
plt.grid(True)

# Guardar la imagen
plt.savefig("speedup.png", dpi=200)
plt.close()

print("Gráfica creada: speedup.png")
