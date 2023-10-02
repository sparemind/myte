import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import scipy.optimize

def sigmoid(x):
    return 1 / (1 + np.exp(-x))
#     x0, y0, c, k=p
#     y = c / (1 + np.exp(-k * (x-x0))) + y0
#     return y


def gradient(w, X, y):
    y_pred = sigmoid(np.dot(X, w))
    error = y_pred - y
    gradient = 2 * np.dot(X.T, error) / len(y)
    return gradient
    
def error(w, X, y):
    y_pred = sigmoid(np.dot(X, w))
    mse = np.mean((y_pred - y)**2)
    return mse

#     y_pred = sigmoid(np.dot(X, w))
#     return (y_pred - y)**2
    
def gradient_descent(X, y, w, learning_rate, iters, step=100):
    for i in range(iters):
        print(f'Iteration {i+1}/{iters}', end='\r')
        grad = gradient(w, X, y)
        w -= learning_rate * grad
        if i % step == 0:
            print(f'Iteration {i+1}/{iters}: {error(w, X, y)}')
    return w


# def residuals(p, x, y):
#     return y - sigmoid(p, x)
#     
# def resize(arr, lower=0.0, upper=1.0):
#     arr = arr.copy()
#     if lower > upper: lower,upper = upper, lower
#     arr -= arr.min()
#     arr *= (upper-lower)/arr.max()
#     arr += lower
#     return arr

def main():
    data = pd.read_csv('training_weights.txt', header=None)
#     print(data[0])
#     return

    x = data.iloc[:, 1:].to_numpy()
    y = data.iloc[:, 0].to_numpy()
    
    n_features = x.shape[1]
    w = np.ones(n_features)
    w = np.array([
    # Baseline
        100,332,343,433,1030,0,0,0,0,0,0,0,0,0,43,42,41,46,29,24,24,30,17,12,17,28,29,19,19,-3,-32,-16,-16,-6,-3,-14,-12,-15,-43,-31,-24,-10,-15,-16,-12,-33,-43,-32,-35,-30,-27,-19,-1,-31,-47,-39,-36,-68,-57,-1,9,-35,0,0,0,0,0,0,0,0,-8,-4,-1,0,3,0,0,-8,-16,-9,8,7,7,8,-4,-11,-8,8,18,15,19,27,10,6,-3,11,9,34,34,58,6,0,-24,1,18,11,30,23,10,-10,-35,-16,1,5,11,6,16,-22,-6,-22,-16,-5,-8,4,-4,-5,-5,-37,-14,-34,-14,-12,-27,-1,0,0,0,-4,0,0,0,0,-16,0,-10,-4,0,5,0,-7,-5,8,-1,5,6,15,11,20,-6,2,3,28,16,19,4,-4,-8,1,11,34,23,9,-4,-9,-1,4,9,1,0,-2,5,4,2,-6,1,-10,0,10,11,-15,-13,-12,-19,-19,-16,-26,-6,-3,3,2,7,12,3,5,10,10,-6,-4,21,24,20,18,8,10,-15,-4,6,11,13,17,16,5,-18,-7,-5,7,9,9,-4,-8,-34,-30,-9,-4,5,4,10,-14,-31,-21,-6,-9,-2,7,6,-6,-49,-13,-14,-6,-2,15,-10,-32,-4,-6,-10,8,13,15,-13,4,-10,3,4,9,16,4,3,2,-12,-16,9,18,9,8,6,16,-23,-7,1,19,24,32,25,31,-12,-8,3,10,21,23,16,16,-13,-9,-9,4,11,12,13,9,-4,-3,-3,-4,4,9,18,2,-16,-16,3,3,2,2,-22,-14,-12,-15,-19,1,-11,-27,-7,-11,-4,-4,0,-4,0,0,5,4,0,6,4,4,1,4,2,4,4,8,9,5,4,3,8,1,2,8,7,10,5,6,6,-6,-1,4,0,3,-1,1,0,-8,-2,-2,-3,-6,-20,-2,-3,-9,-4,-3,3,-19,-39,-30,16,15,-18,50,6,-22,-8,-56,34,20,24,125,210,243,454,725,0,0,0,0,0,0,0,0,0,90,104,93,86,68,85,74,91,34,57,44,29,32,19,20,20,-4,-7,-23,-34,-40,-29,-6,-23,-18,-34,-34,-45,-45,-39,-24,-33,-31,-30,-41,-33,-36,-35,-34,-36,-20,-20,-27,-28,-19,-34,-25,-35,0,0,0,0,0,0,0,0,-8,-5,-5,0,0,-5,-5,-12,-13,-2,8,5,9,-5,-11,-10,-15,2,26,20,15,22,3,-6,-7,5,22,31,0,18,7,-7,-7,-13,21,19,15,9,-5,-5,-11,-11,0,10,5,-5,-5,-13,-9,-16,-21,-10,-2,3,-1,-9,-13,-16,-20,-22,-10,-15,-11,-5,-2,-6,0,-8,3,-1,-1,-1,-6,4,0,-1,1,0,4,4,1,3,-2,1,1,21,10,-2,-4,9,8,11,8,8,-3,8,-3,3,17,9,4,3,-8,-10,-2,-7,15,21,17,-2,-11,-3,-10,-5,-1,5,-14,-4,-20,-15,-11,-8,-24,-20,-8,-19,-6,-5,6,15,11,12,11,12,8,10,15,19,15,18,18,16,13,14,14,15,14,12,12,17,13,2,11,11,15,11,10,11,2,4,3,4,3,4,-3,-1,-10,-24,2,-13,-14,-14,-15,-11,-16,-25,-25,-12,-15,-14,-15,-10,-24,-27,-18,-16,-11,-17,-21,-22,-25,-38,-21,-4,2,13,13,-4,-11,-9,-6,-4,9,15,13,4,4,2,-14,1,6,19,22,16,7,10,-5,2,5,13,22,21,11,11,-7,-3,0,17,2,8,2,0,-4,-7,-7,-7,-7,-8,0,-3,-7,-14,-14,-24,-15,-16,-18,-12,-10,-19,-22,-35,-23,-32,-23,-33,-34,-28,-8,-21,-12,5,19,16,-7,14,15,7,5,24,14,10,4,6,23,5,6,8,21,-1,2,16,22,17,13,24,7,-12,-10,0,21,11,19,18,0,-21,-11,-6,10,6,21,13,-4,-16,-18,-12,8,7,11,13,-8,-21,-37,-41,-16,-20,-19,-5,-32,-40,41
    ], dtype='float')
    initial_err = error(w, x, y)
    print('Feature Count: ', n_features, len(w))
    print('Initial Error: ', initial_err)
    print('Optimizing...', end='')
    w = gradient_descent(x, y, w, learning_rate=0.005, iters=5000, step=500)
    mse = error(w, x, y)
    # normalize weights so that a midgame pawn is 100
    w = w / w[0]*100 
    print(f'Done, improved by {initial_err - mse} to {mse}')
    
    
    
    
    results = []
    
    for game_phase in range(2):
        results.append(w[:6])
        w = w[6:]
        
        for i in range(6):
            results.append(w[:64])
            w = w[64:]
            
        results.append(w[:1])
        w = w[1:]
    
    for result in results:
        print('[', end='')
        for i, x in enumerate(result):
            print(int(x), end=',' if i < len(result)-1 else '')
        print('],')
    
    print()
    return
    
#     print(x[0][:5], y[0], len(x[0]))
#     return
    
#     x = np.array([
#         [0, 0, 0],
#         [0, 0, 0],
#         
#         [1, 1, 1],
#         [2, 2, 0],
#         
#         [1, 1, 2],
#         [0, 3, 4],
#         ], dtype='float')
#     y = np.array([0, 0, 0.5, 0.5, 1, 1], dtype='float')
    
    initial_w = np.ones(len(x[0])) 
#     result = scipy.optimize.least_squares(error, initial_w, args=(x,y))
    
    print('Optimizing...', end='')
    result = scipy.optimize.minimize(error, initial_w, args=(x,y), method='BFGS')
    print('Done')
    
    optimal_weights = result.x
    print(optimal_weights)
    
    
#     x = resize(-x, lower=0.3)
#     y = resize(y, lower=0.3)
    
#     p_guess = (np.median(x), np.median(y), 1.0, 1.0)
#     p, cov, info, mesg, ier = scipy.optimize.leastsq(residuals, p_guess, args=(x, y), full_output=True)
#     print(p)

#     py = [sigmoid(np.dot(xi, optimal_weights)) for xi in x]
#     print(py)
    
#     plt.plot(x, y, '.')
    ax = plt.gca()
    ax.set_xlim([-10, 10])
    ax.set_ylim([-10, 10])
    
    plt.plot(y, py, 'o')
    plt.xlabel('x')
    plt.ylabel('y')
    plt.grid(True)
    plt.show()
    
if __name__ == "__main__":
    main()
